using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoAtacar : State
    {
        public EstadoAtacar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            AtaqueDistancia disparar = Agente.GetComponent<AtaqueDistancia>();

            if(disparar != null && Pizarra.Get(ClavesBB.Rango, true) && Pizarra.Get(ClavesBB.Visible, true))
            {

            }
            else
            {

            }
        }
        internal override void Execute()
        {
            AtaqueDistancia disparar = Agente.GetComponent<AtaqueDistancia>();
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Alerta);

            if (nivel == NivelSospecha.Sospecha)
            {
                // TODO · descomentad cuando tengáis EstadoInvestigar
                // StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }
            if (disparar.balas == 0)
            {
                disparar.Recargar();
            }

            if (Pizarra.Get(ClavesBB.Rango, true) && disparar.balas > 0)
            {
                disparar.Disparar();
            }

        }
        internal override void Exit()
        {
            // Aquí no hay nada que limpiar. Pero el método existe y está vacío
            // a propósito: cuando en Enter() arranquéis una animación, un bark
            // o una corrutina, Exit() es donde se apaga. Si no lo hacéis, os
            // quedaréis con guardias diciendo «¿quién anda ahí?» mientras
            // persiguen. Pasa todos los años.
        }

    }

}
