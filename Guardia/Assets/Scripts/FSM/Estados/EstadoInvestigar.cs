// ═══════════════════════════════════════════════════════════════════════════
//  EstadoInvestigar.cs — El guardia SOSPECHA: «¿qué ha sido eso?»
//
//  Se queda parado un momento (PausaInvestigar) y después se acerca hacia lo
//  que ha visto, pero SIN COMPROMETERSE: como mucho AvanceInvestigar metros.
//  Al llegar se queda quieto mirando hasta que la sospecha suba o se calme.
//
//  Transiciones (todas leyendo la pizarra):
//    · nivel == Alerta    → Perseguir
//    · nivel == Tranquilo → Patrullar (y olvida la última posición)
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoInvestigar : State
    {
        public EstadoInvestigar(StateMachine machine) : base(machine) { }

        // En el patrón Estado cada estado es un objeto nuevo en cada
        // ChangeState, así que sus campos son la memoria de ESTA visita.
        private float _inicio;
        private bool _haPedidoDestino;
        private Vector3 _vistoAlPedir;

        internal override void Enter()
        {
            _inicio = Pizarra.Tiempo;

            // Primero se para en seco.
            Pizarra.Set(ClavesBB.TieneDestino, false);
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            // ── 1. Transiciones
            if (nivel == NivelSospecha.Alerta)
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }

            // Hay una búsqueda a medias (p. ej. tras cambiar del árbol a la FSM):
            // misma prioridad que el árbol, Buscar va antes que Investigar.
            if (Pizarra.Get(ClavesBB.BusquedaPendiente, false))
            {
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                return;
            }

            if (nivel == NivelSospecha.Tranquilo)
            {
                // Falsa alarma: lo que vio ya no cuenta.
                Pizarra.Set(ClavesBB.TieneUltimaPos, false);
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
                return;
            }

            // ── 2. Pausa inicial: quieto, «mirando»
            if (Pizarra.Tiempo - _inicio < Agente.PausaInvestigar) return;

            if (!Pizarra.Get(ClavesBB.TieneUltimaPos, false)) return;

            // ── 3. Acercarse un poco hacia lo que vio, sin llegar a ir del todo (Meterlo en el locomotion)
            Vector3 yo = Agente.transform.position;
            Vector3 visto = Pizarra.Get(ClavesBB.UltimaPos, yo);
            Vector3 haciaVisto = visto - yo;
            haciaVisto.y = 0f;

            Vector3 destino = haciaVisto.magnitude <= Agente.AvanceInvestigar
                ? visto
                : yo + haciaVisto.normalized * Agente.AvanceInvestigar;

            // Solo pedimos un destino nuevo la primera vez o si lo que vio se ha
            // movido más de 1 m. Si lo recalculáramos cada tick desde la nueva
            // posición, el «avance máximo» nunca se acabaría y no pararía.
            if (!_haPedidoDestino || (visto - _vistoAlPedir).sqrMagnitude > 1f)
            {
                Pizarra.Set(ClavesBB.Destino, destino);
                Pizarra.Set(ClavesBB.TieneDestino, true);
                Pizarra.Set(ClavesBB.HaLlegado, false);

                _haPedidoDestino = true;
                _vistoAlPedir = visto;
            }
        }

        internal override void Exit() { }
    }
}
