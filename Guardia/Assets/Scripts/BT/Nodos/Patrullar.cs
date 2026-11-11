// ═══════════════════════════════════════════════════════════════════════════
//  Patrullar.cs — Recorrer la ruta de waypoints
//
//  Es la versión «con nombre» del ActionNode de ejemplo del profesor, con una
//  mejora: OnInitialize.
//
//  Problema del ejemplo original: solo pedía un punto nuevo cuando HaLlegado
//  era true. Al volver de una búsqueda, el guardia seguía caminando hacia el
//  último punto de búsqueda antes de retomar la ruta. OnInitialize se ejecuta
//  cada vez que la rama de patrulla vuelve a empezar (después de que otra rama
//  la haya abortado), y ahí apuntamos directamente al waypoint que toca. Es el
//  equivalente al Enter() de EstadoPatrullar en la FSM.
//
//  Devuelve siempre Running: patrullar es la acción de fondo, no termina nunca.
// ═══════════════════════════════════════════════════════════════════════════

using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class Patrullar : NodoMovimiento
    {
        private readonly RutaPatrulla _ruta;

        public Patrullar(string nombre, RutaPatrulla ruta) : base(nombre)
        {
            _ruta = ruta;
        }

        protected override void OnInitialize(Blackboard bb)
        {
            base.OnInitialize(bb);   // reinicia la sincronización de NodoMovimiento

            if (_ruta != null && _ruta.TieneRuta)
                PedirDestino(bb, _ruta.PuntoActual);
        }

        protected override Status Update(Blackboard bb)
        {
            // Sin ruta no se puede patrullar: es un fallo de montaje de escena
            // (README_Montaje, apartados 3.4 y 3.5), no de código.
            if (_ruta == null || !_ruta.TieneRuta) return Status.Failure;

            if (HaLlegado(bb))
                PedirDestino(bb, _ruta.Avanzar());

            return Status.Running;
        }
    }
}
