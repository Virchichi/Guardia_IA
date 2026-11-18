// ═══════════════════════════════════════════════════════════════════════════
//  AccionesPatrulla.cs — Nodo hoja de la rama de patrulla
//
//  La patrulla ya no es un nodo gigante: se monta en DecisorBT combinando
//  piezas pequeñas, igual que la FSM combina EstadoPatrullar y EstadoIdle:
//
//    RepetirSiempre
//      └── Sequence "Ronda"
//            ├── IrA (waypoint actual)       ← EstadoPatrullar
//            ├── AvanzarRuta                 ← EstadoPatrullar, al llegar
//            ├── Detenerse                   ← EstadoIdle.Enter
//            └── Esperar (TiempoDescanso)    ← EstadoIdle
// ═══════════════════════════════════════════════════════════════════════════

using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    /// <summary>
    /// Pasa al siguiente waypoint de la ruta. No mueve: solo cambia cuál es el
    /// «punto actual». El siguiente IrA de la ronda irá a por él. Siempre Success.
    /// </summary>
    public class AvanzarRuta : Node
    {
        private readonly RutaPatrulla _ruta;

        public AvanzarRuta(string nombre, RutaPatrulla ruta) : base(nombre)
        {
            _ruta = ruta;
        }

        protected override Status Update(Blackboard bb)
        {
            if (_ruta != null && _ruta.TieneRuta) _ruta.Avanzar();
            return Status.Success;
        }
    }
}
