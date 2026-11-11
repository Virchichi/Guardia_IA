// ═══════════════════════════════════════════════════════════════════════════
//  IrA.cs — Ir a un punto y TERMINAR al llegar
//
//  La diferencia con Seguir: IrA devuelve Success cuando llega. Por eso puede
//  ir dentro de una Sequence («ve allí y DESPUÉS haz otra cosa»), cosa que con
//  Seguir no se podría, porque Seguir nunca termina.
//
//    Running → todavía en camino
//    Success → he llegado
// ═══════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class IrA : NodoMovimiento
    {
        private readonly Func<Blackboard, Vector3> _destino;

        /// <param name="destino">Lambda que dice a dónde ir, leyendo la pizarra.
        /// Por ejemplo: bb =&gt; bb.Get(ClavesBB.UltimaPos, Vector3.zero)</param>
        public IrA(string nombre, Func<Blackboard, Vector3> destino) : base(nombre)
        {
            _destino = destino;
        }

        protected override Status Update(Blackboard bb)
        {
            PedirDestino(bb, _destino(bb));

            return HaLlegado(bb) ? Status.Success : Status.Running;
        }
    }
}
