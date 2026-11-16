// ═══════════════════════════════════════════════════════════════════════════
//  Condiciones.cs — Nodos hoja que solo PREGUNTAN a la pizarra
//
//  Una condición nunca cambia nada: lee y responde Success (sí) o Failure
//  (no). Nunca devuelve Running. Y nunca mira el mundo (nada de Raycast ni
//  de GetComponent<SensorVision>): todo lo que sabe, lo sabe por la pizarra.
//  Esa es la regla nº 1 del enunciado: «la percepción escribe, la decisión lee».
// ═══════════════════════════════════════════════════════════════════════════

using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    /// <summary>
    /// ¿La clave booleana <c>clave</c> vale <c>valorEsperado</c>?
    /// Ejemplos: «¿veo al jugador?», «¿NO veo al jugador?», «¿tengo última posición?».
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
    /// ¿El nivel de sospecha es AL MENOS <c>minimo</c>?
    ///
    /// Lee ClavesBB.Nivel (el enum con la histéresis ya aplicada por el sensor)
    /// y NO el float de sospecha en bruto. Si comparásemos el float con
    /// umbrales propios, nos saltaríamos la histéresis y el guardia parpadearía
    /// entre ramas.
    ///
    /// «Al menos» funciona porque el enum está ordenado:
    /// Tranquilo (0) &lt; Sospecha (1) &lt; Alerta (2).
    /// </summary>
    public class CondicionNivelAlMenos : Node
    {
        private readonly NivelSospecha _minimo;

        public CondicionNivelAlMenos(string nombre, NivelSospecha minimo) : base(nombre)
        {
            _minimo = minimo;
        }

        protected override Status Update(Blackboard bb)
        {
            NivelSospecha actual = bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            return actual >= _minimo ? Status.Success : Status.Failure;
        }
    }
}
