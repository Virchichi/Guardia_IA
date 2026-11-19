// ═══════════════════════════════════════════════════════════════════════════
//  EstadoAtacar.cs — En ALERTA, lo ve y está a tiro: se para y dispara
//
//  La decisión NO dispara: solo PIDE disparar escribiendo en la pizarra
//  (quiereDisparar + objetivoDisparo). Quien gira al guardia, gestiona el
//  cargador y crea la bala es AtaqueDistancia, en la capa de acción. Es el
//  mismo contrato que el movimiento: la decisión pide un destino y
//  LocomocionNavMesh lo ejecuta.
//
//  Transiciones (todas leyendo la pizarra):
//    · nivel baja de Alerta                  → Buscar
//    · deja de verlo                         → Buscar (empieza yendo a la última posición)
//    · se aleja más de Distancia + Margen    → Perseguir
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoAtacar : State
    {
        public EstadoAtacar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            // Para disparar se queda quieto.
            Pizarra.Set(ClavesBB.TieneDestino, false);
            Pizarra.Set(ClavesBB.BusquedaPendiente, true);
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            bool visible = Pizarra.Get(ClavesBB.Visible, false);

            // ── 1. Se ha calmado
            if (nivel != NivelSospecha.Alerta)
            {
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                return;
            }

            // ── 2. Ya no lo ve: a perseguir hasta donde lo vio por última vez
            if (!visible)
            {
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                return;
            }

            // ── 3. Se ha alejado demasiado (con margen, para no alternar en el límite)
            Vector3 objetivo = Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position);
            float distancia = Vector3.Distance(Agente.transform.position, objetivo);
            if (distancia > Agente.DistanciaDisparo + Agente.MargenRango)
            {
                StateMachine.ChangeState(new EstadoPerseguir(StateMachine));
                return;
            }

            // ── 4. Pedir disparo: quieto, apuntando a donde lo ve
            Pizarra.Set(ClavesBB.TieneDestino, false);
            Pizarra.Set(ClavesBB.ObjetivoDisparo, objetivo);
            Pizarra.Set(ClavesBB.QuiereDisparar, true);
        }

        internal override void Exit()
        {
            // Lo que se enciende en el estado se apaga al salir: si no, seguiría
            // disparando mientras persigue o busca.
            Pizarra.Set(ClavesBB.QuiereDisparar, false);
        }
    }
}
