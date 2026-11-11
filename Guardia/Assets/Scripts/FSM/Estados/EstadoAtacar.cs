using UnityEngine;

namespace UDIT.IA3.Guardia
{
    public class EstadoAtacar : State
    {
        public EstadoAtacar(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {
            AtaqueDistancia disparar = Agente.GetComponent<AtaqueDistancia>();

            if(disparar == null )
            {
                //Error
            }

            //Si dispara, se queda quieto 
            Pizarra.Get(ClavesBB.TieneDestino, false);

        }
        internal override void Execute()
        {
            AtaqueDistancia disparar = Agente.GetComponent<AtaqueDistancia>();
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Alerta);
            bool enRango = Pizarra.Get(ClavesBB.Rango, true);
        

            if (nivel == NivelSospecha.Sospecha)
            {
                // TODO · descomentad cuando tengáis EstadoInvestigar
                // StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }

            //Rango
            if(!enRango)
            {
                if(Pizarra.Get(ClavesBB.TieneUltimaPos,false))
                {
                    Pizarra.Get(ClavesBB.Destino, Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position));
                    Pizarra.Get(ClavesBB.TieneDestino, true);
                }
            }

            Pizarra.Get(ClavesBB.TieneDestino, false);
            if (disparar.balas == 0)
            {
                disparar.Recargar();
            }

            if (disparar.balas > 0)
            {
                Vector3 Objetivo = Pizarra.Get(ClavesBB.UltimaPos, Agente.transform.position);
                disparar.Disparar(Objetivo);
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
