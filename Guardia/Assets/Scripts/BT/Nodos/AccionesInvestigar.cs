// ═══════════════════════════════════════════════════════════════════════════
//  AccionesInvestigar.cs — «¿Qué ha sido eso?»: acercarse sin comprometerse
//
//  Equivale al paso 3 de EstadoInvestigar: se acerca hacia lo que ha visto,
//  pero como mucho AvanceInvestigar metros desde donde está. Al llegar se
//  queda quieto mirando hasta que la sospecha suba (Alerta) o se calme
//  (Tranquilo). Por eso devuelve siempre Running: lo corta el Monitor.
//
//  La pausa inicial (PausaInvestigar) no está aquí: es un nodo Esperar que va
//  delante en la Sequence (ver DecisorBT.RamaInvestigar).
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class AcercarseConCautela : NodoMovimiento
    {
        /// <summary>Si lo que vio se mueve más de esto (1 m, al cuadrado), se recalcula el destino.</summary>
        private const float CambioMinimoCuadrado = 1f;

        private readonly GuardiaAgente _agente;

        // Memoria de ESTA ejecución del nodo (se reinicia en OnInitialize).
        // No dice «en qué estado está el guardia», sino a qué punto se pidió
        // ir, para no recalcularlo cada tick. Es lo mismo que guardan los
        // campos _haPedidoDestino y _vistoAlPedir de EstadoInvestigar.
        private bool _haPedido;
        private Vector3 _vistoAlPedir;

        public AcercarseConCautela(string nombre, GuardiaAgente agente) : base(nombre)
        {
            _agente = agente;
        }

        protected override void OnInitialize(Blackboard bb)
        {
            _haPedido = false;
        }

        protected override Status Update(Blackboard bb)
        {
            if (!bb.Get(ClavesBB.TieneUltimaPos, false)) return Status.Running;

            Vector3 yo = _agente.transform.position;
            Vector3 visto = bb.Get(ClavesBB.UltimaPos, yo);
            Vector3 haciaVisto = visto - yo;
            haciaVisto.y = 0f;

            // Como mucho AvanceInvestigar metros: no se compromete.
            Vector3 destino = haciaVisto.magnitude <= _agente.AvanceInvestigar
                ? visto
                : yo + haciaVisto.normalized * _agente.AvanceInvestigar;

            // Solo se pide destino la primera vez o si lo visto se ha movido
            // más de 1 m. Si se recalculara cada tick desde la posición nueva,
            // el «avance máximo» no se acabaría nunca y no se pararía.
            if (!_haPedido || (visto - _vistoAlPedir).sqrMagnitude > CambioMinimoCuadrado)
            {
                PedirDestino(bb, destino);
                _haPedido = true;
                _vistoAlPedir = visto;
            }

            return Status.Running;
        }
    }
}
