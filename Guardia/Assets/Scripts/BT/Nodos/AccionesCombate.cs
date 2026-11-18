// ═══════════════════════════════════════════════════════════════════════════
//  AccionesCombate.cs — Disparar: pedir el disparo, no hacerlo
//
//  Mismo contrato que el movimiento: la decisión PIDE y la capa de acción
//  HACE. Este nodo solo escribe en la pizarra QuiereDisparar y
//  ObjetivoDisparo. Quien gira al guardia, respeta la cadencia, recarga y
//  crea la bala es AtaqueDistancia. Por eso el árbol no tiene una rama de
//  «Recargar»: la recarga es trabajo de la capa de acción, igual que con la
//  FSM.
//
//  Equivale a EstadoAtacar:
//    Enter   → quieto
//    Execute → quieto, apuntando a donde lo ve, pidiendo disparo
//    Exit    → deja de pedir disparo
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class Disparar : Node
    {
        public Disparar(string nombre) : base(nombre) { }

        protected override void OnInitialize(Blackboard bb)
        {
            // Para disparar se queda quieto.
            bb.Set(ClavesBB.TieneDestino, false);
        }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.TieneDestino, false);
            bb.Set(ClavesBB.ObjetivoDisparo, bb.Get(ClavesBB.UltimaPos, Vector3.zero));
            bb.Set(ClavesBB.QuiereDisparar, true);

            // Disparar dura mientras la guarda se cumpla: siempre Running.
            return Status.Running;
        }

        protected override void OnTerminate(Blackboard bb, Status resultado)
        {
            // Lo que se enciende se apaga al salir. Si no, AtaqueDistancia
            // seguiría disparando mientras el guardia persigue o busca.
            bb.Set(ClavesBB.QuiereDisparar, false);
        }
    }
}
