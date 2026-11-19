using UnityEngine;
namespace UDIT.IA3.Guardia
{
    public class EstadoIdle : State
    {
        public EstadoIdle(StateMachine machine) : base(machine) { }

        internal override void Enter()
        {

            Pizarra.Set(ClavesBB.TieneDestino, false);
            Pizarra.Set(ClavesBB.InicioDescanso, Pizarra.Tiempo);
        }

        internal override void Execute()
        {
            NivelSospecha nivel = Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

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

            if (nivel == NivelSospecha.Sospecha)
            {
                StateMachine.ChangeState(new EstadoInvestigar(StateMachine));
                return;
            }

            float inicioDescanso = Pizarra.Get(ClavesBB.InicioDescanso, Pizarra.Tiempo);
            float tiempoDescanso = Pizarra.Tiempo - inicioDescanso;

            if (tiempoDescanso >= Agente.TiempoDescanso)
            {
                StateMachine.ChangeState(new EstadoPatrullar(StateMachine));
                return;
            }

        }

        internal override void Exit()
        {

        }
    }
}
