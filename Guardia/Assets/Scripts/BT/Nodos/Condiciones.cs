// ═══════════════════════════════════════════════════════════════════════════
//  Condiciones.cs — Nodos hoja que solo PREGUNTAN a la pizarra
//
//  Una condición nunca cambia nada: lee y responde Success (sí) o Failure
//  (no). Nunca devuelve Running. Y nunca mira el mundo: nada de Raycast ni de
//  GetComponent<SensorVision>(). Todo lo que sabe lo sabe por la pizarra.
//  Es la regla nº 1: «la percepción escribe, la decisión lee».
//
//  Se usan sobre todo como GUARDAS de un Monitor, que las vuelve a mirar en
//  cada tick aunque el cuerpo siga en Running.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    /// <summary>
    /// ¿La clave booleana <c>clave</c> vale <c>valorEsperado</c>?
    /// Ejemplos: «¿lo veo?», «¿hay búsqueda pendiente?».
    /// </summary>
    public class CondicionClave : Node
    {
        private readonly string _clave;
        private readonly bool _valorEsperado;

        public CondicionClave(string nombre, string clave, bool valorEsperado = true)
            : base(nombre)
        {
            _clave = clave;
            _valorEsperado = valorEsperado;
        }

        protected override Status Update(Blackboard bb)
        {
            return bb.Get(_clave, false) == _valorEsperado ? Status.Success : Status.Failure;
        }
    }

    /// <summary>
    /// ¿El nivel de sospecha es exactamente <c>nivel</c>?
    ///
    /// Lee ClavesBB.Nivel (el tramo con la histéresis ya aplicada por el
    /// sensor) y NO el float de sospecha en bruto. Si comparásemos el float con
    /// umbrales propios, nos saltaríamos la histéresis y el guardia parpadearía
    /// entre ramas (apuntes B3.3a, diapositiva 8).
    /// </summary>
    public class CondicionNivel : Node
    {
        private readonly NivelSospecha _nivel;

        public CondicionNivel(string nombre, NivelSospecha nivel) : base(nombre)
        {
            _nivel = nivel;
        }

        protected override Status Update(Blackboard bb)
        {
            return bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == _nivel
                ? Status.Success : Status.Failure;
        }
    }

    /// <summary>
    /// ¿El jugador está a tiro?
    ///
    /// Usa los mismos valores que la FSM (GuardiaAgente.DistanciaDisparo y
    /// MargenRango) y la misma HISTÉRESIS de rango:
    ///   · si todavía no está disparando, tiene que estar a DistanciaDisparo o menos;
    ///   · si ya está disparando, sigue hasta que se aleje más de
    ///     DistanciaDisparo + MargenRango.
    /// Sin ese margen, un jugador justo en el límite haría que el guardia
    /// alternase entre disparar y perseguir varias veces por segundo.
    ///
    /// ¿Cómo sabe si «ya está disparando» sin guardar estado? Lo pregunta a la
    /// pizarra: QuiereDisparar es la intención que escribe el nodo Disparar.
    ///
    /// La posición del propio guardia sale de su transform: conocer tu propia
    /// posición no es percibir (no mira al jugador ni lanza rayos). La FSM hace
    /// exactamente lo mismo en EstadoPerseguir y EstadoAtacar.
    /// </summary>
    public class CondicionEnRangoDeDisparo : Node
    {
        private readonly GuardiaAgente _agente;

        public CondicionEnRangoDeDisparo(string nombre, GuardiaAgente agente) : base(nombre)
        {
            _agente = agente;
        }

        protected override Status Update(Blackboard bb)
        {
            Vector3 yo = _agente.transform.position;
            Vector3 objetivo = bb.Get(ClavesBB.UltimaPos, yo);
            float distancia = Vector3.Distance(yo, objetivo);

            bool yaDisparando = bb.Get(ClavesBB.QuiereDisparar, false);
            float limite = yaDisparando
                ? _agente.DistanciaDisparo + _agente.MargenRango
                : _agente.DistanciaDisparo;

            return distancia <= limite ? Status.Success : Status.Failure;
        }
    }
}
