// ═══════════════════════════════════════════════════════════════════════════
//  EstadoPerseguir.cs — El guardia está en ALERTA: va a por el jugador
//
//  Mientras lo ve, el sensor actualiza ultimaPos en cada frame, así que ir a
//  ultimaPos es ir a por él. Si deja de verlo, sigue yendo a la ÚLTIMA
//  posición conocida; al llegar sin verlo, pasa a Buscar.
//
//  Transiciones (todas leyendo la pizarra):
//    · nivel baja de Alerta          → Buscar (o Patrullar si no hay ultimaPos)
//    · no visible y ha llegado       → Buscar
//    · visible y a DistanciaDisparo  → Atacar
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoPerseguir : State
    {
        public EstadoPerseguir(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            // HaLlegado puede seguir a true de cuando estaba parado. Lo
            // reseteamos para que el primer Execute no crea que ya ha llegado.
            Pizarra.Set(ClavesBB.HaLlegado, false);
            PedirIrAUltimaPos();
            AnimacionGuardia.Andar(Agente.Animator);
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            bool tieneUltimaPos = Pizarra.Get(ClavesBB.TieneUltimaPos, false);

            // ── 1. ¿Se ha calmado? (la histéresis ya está aplicada en el sensor)
            if (nivel != NivelSospecha.Alerta)
            {
                if (tieneUltimaPos) StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                else                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
                return;
            }

            // ── 2. Lo he perdido de vista y ya estoy donde lo vi → buscar
            bool visible = Pizarra.Get(ClavesBB.Visible, false);
            if (!visible && Pizarra.Get(ClavesBB.HaLlegado, false))
            {
                StateMachine.ChangeState(new EstadoBuscar(StateMachine));
                return;
            }

            // ── 3. Lo veo y está a tiro → parar y disparar
            if (visible)
            {
                Vector3 objetivo = Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position);
                if (Vector3.Distance(Agente.transform.position, objetivo) <= Agente.DistanciaDisparo)
                {
                    StateMachine.ChangeState(new EstadoAtacar(StateMachine));
                    return;
                }
            }

            // ── 4. Seguir persiguiendo. La locomoción solo recalcula la ruta
            //       si el destino ha cambiado de verdad.
            PedirIrAUltimaPos();
            AnimacionGuardia.Andar(Agente.Animator);
        }

        internal override void Exit() { }

        private void PedirIrAUltimaPos()
        {
            if (!Pizarra.Get(ClavesBB.TieneUltimaPos, false)) return;

            Pizarra.Set(ClavesBB.Destino, Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position));
            Pizarra.Set(ClavesBB.TieneDestino, true);
        }
    }
}
