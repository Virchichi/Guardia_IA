// ═══════════════════════════════════════════════════════════════════════════
//  DecisorFSM.cs — Cerebro versión 1: máquina de estados
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  APARTADO B DEL ENUNCIADO.
//
//  Aquí solo se monta la máquina y se arranca. El comportamiento vive en los
//  estados, en Scripts/FSM/Estados/. Os doy EstadoPatrullar entero como
//  ejemplo del patrón; los otros tres son vuestros.
// ═══════════════════════════════════════════════════════════════════════════

using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class DecisorFSM : IDecisor
    {
        private readonly GuardiaAgente _agente;
        private StateMachine _maquina;

        public DecisorFSM(GuardiaAgente agente) { _agente = agente; }

        public void Iniciar(Blackboard bb)
        {
            _maquina = new StateMachine(_agente, bb);
            _maquina.Start(EstadoSegunPizarra(bb));
        }

        public void Decidir(Blackboard bb)
        {
            _maquina.Execute();
        }

        /// <summary>
        /// Estado inicial deducido de la pizarra, con la MISMA prioridad que el
        /// ActiveSelector del árbol (Combate > Buscar > Investigar > Patrullar).
        /// Así, al cambiar del árbol a la FSM en caliente, la FSM continúa lo
        /// que estaba haciendo el guardia en vez de volver siempre a patrullar.
        /// No se guarda «en qué estado estaba»: se calcula de lo que sabe.
        /// </summary>
        private State EstadoSegunPizarra(Blackboard bb)
        {
            NivelSospecha nivel = bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
            bool visible = bb.Get(ClavesBB.Visible, false);

            if (nivel == NivelSospecha.Alerta && visible)
                return new EstadoPerseguir(_maquina);      // pasa a Atacar si está a tiro
            if (bb.Get(ClavesBB.BusquedaPendiente, false) || nivel == NivelSospecha.Alerta)
                return new EstadoBuscar(_maquina);
            if (nivel == NivelSospecha.Sospecha)
                return new EstadoInvestigar(_maquina);
            return new EstadoPatrullar(_maquina);
        }

        public string Traza
        {
            get { return "FSM: " + (_maquina == null ? "-" : _maquina.CurrentName); }
        }
    }
}
