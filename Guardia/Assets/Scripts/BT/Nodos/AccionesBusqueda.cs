// ═══════════════════════════════════════════════════════════════════════════
//  AccionesBusqueda.cs — Nodos hoja de la rama de búsqueda
//
//  Equivale a EstadoBuscar de la FSM: ir a la última posición conocida,
//  revisar PuntosBusqueda puntos en círculo alrededor (radio RadioBusqueda),
//  parándose PausaBusqueda segundos en cada uno, y rendirse si se acaban los
//  puntos o el tiempo (TiempoBusqueda).
//
//  En el árbol se monta así (ver DecisorBT.RamaBuscar):
//
//    Sequence "Busqueda"
//      ├── MarcarBusquedaPendiente
//      ├── Succeeder
//      │     └── LimiteDeTiempo (TiempoBusqueda)
//      │           └── Sequence "RecorrerZona"
//      │                 ├── IrA (ultimaPos) → Detenerse → Esperar
//      │                 ├── PrepararPuntosDeBusqueda
//      │                 └── Repeat x PuntosBusqueda
//      │                       └── ElegirPuntoDeBusqueda → IrA → Detenerse → Esperar
//      └── TerminarBusqueda
//
//  ¿Dónde está el «por qué punto voy»? En la pizarra (AnguloBusqueda), no en
//  un nodo. Los nodos no guardan en qué paso de la búsqueda están: eso lo
//  dicen la forma del árbol (Sequence, Repeat) y la pizarra.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    /// <summary>
    /// Apunta en la pizarra que hay una búsqueda pendiente: «lo he tenido en
    /// alerta y todavía no he terminado de buscarlo». Siempre Success.
    ///
    /// Hace falta porque la FSM sigue buscando aunque la sospecha baje a
    /// Sospecha o a Tranquilo: una vez en EstadoBuscar, solo sale al terminar
    /// la búsqueda o al volver a verlo en alerta. La FSM lo «recuerda» porque
    /// está en ese estado. El árbol no tiene estado actual: necesita que ese
    /// recuerdo esté en la pizarra. (Material para la comparativa §5.E.)
    /// </summary>
    public class MarcarBusquedaPendiente : Node
    {
        public MarcarBusquedaPendiente(string nombre) : base(nombre) { }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.BusquedaPendiente, true);
            return Status.Success;
        }
    }

    /// <summary>
    /// Elige al azar el ángulo del primer punto del círculo, para que no busque
    /// siempre igual (como GenerarPuntos en EstadoBuscar). Siempre Success.
    /// </summary>
    public class PrepararPuntosDeBusqueda : Node
    {
        public PrepararPuntosDeBusqueda(string nombre) : base(nombre) { }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.AnguloBusqueda, Random.Range(0f, 360f));
            return Status.Success;
        }
    }

    /// <summary>
    /// Calcula el siguiente punto del círculo alrededor de la última posición
    /// conocida, lo escribe en ClavesBB.PuntoBusqueda (para que vaya un IrA) y
    /// avanza el ángulo para el próximo. Siempre Success.
    ///
    /// Se calcula con matemáticas, sin Raycast: si el punto cae dentro de un
    /// muro, el NavMeshAgent irá al punto alcanzable más cercano. Igual que en
    /// la FSM.
    ///
    /// ¿Por qué no calcular el punto dentro de la lambda del IrA? Porque el IrA
    /// evalúa su lambda una vez al empezar, pero el ángulo tiene que avanzar
    /// exactamente una vez por punto. Elegir e ir son dos pasos distintos.
    /// </summary>
    public class ElegirPuntoDeBusqueda : Node
    {
        private readonly GuardiaAgente _agente;

        public ElegirPuntoDeBusqueda(string nombre, GuardiaAgente agente) : base(nombre)
        {
            _agente = agente;
        }

        protected override Status Update(Blackboard bb)
        {
            int cantidad = Mathf.Max(1, _agente.PuntosBusqueda);
            float angulo = bb.Get(ClavesBB.AnguloBusqueda, 0f);
            Vector3 centro = bb.Get(ClavesBB.UltimaPos, _agente.transform.position);

            float rad = angulo * Mathf.Deg2Rad;
            Vector3 punto = centro + new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * _agente.RadioBusqueda;

            bb.Set(ClavesBB.PuntoBusqueda, punto);
            bb.Set(ClavesBB.AnguloBusqueda, angulo + 360f / cantidad);
            return Status.Success;
        }
    }

    /// <summary>
    /// «He buscado y no hay nadie»: olvida la última posición (caducidad) y
    /// borra la búsqueda pendiente. Al hacerlo, la guarda de la rama Buscar
    /// deja de cumplirse y el guardia vuelve a patrullar. Es el Rendirse() de
    /// EstadoBuscar. Siempre Success.
    ///
    /// Según la tabla del enunciado, tieneUltimaPos la escriben el «sensor /
    /// acción de buscar» y caduca «al llegar y no ver nada». Esta es esa
    /// acción de buscar. Sin ella el guardia recordaría la posición para
    /// siempre (error típico: «sin caducidad»).
    /// </summary>
    public class TerminarBusqueda : Node
    {
        public TerminarBusqueda(string nombre) : base(nombre) { }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.TieneUltimaPos, false);
            bb.Set(ClavesBB.BusquedaPendiente, false);
            return Status.Success;
        }
    }
}
