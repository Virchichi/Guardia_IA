// ═══════════════════════════════════════════════════════════════════════════
//  AccionesComunes.cs — Nodos hoja pequeños que se usan en varias ramas
//
//    Detenerse → «quédate quieto» (retira el destino). Termina al instante.
//    Esperar   → deja pasar N segundos. Dura: devuelve Running mientras tanto.
// ═══════════════════════════════════════════════════════════════════════════

using System;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    /// <summary>
    /// Pide a la locomoción que se pare (TieneDestino = false). Es lo que hace
    /// la FSM en el Enter de Idle, Investigar y Atacar, y al llegar a cada
    /// punto de búsqueda. Siempre Success.
    /// </summary>
    public class Detenerse : Node
    {
        public Detenerse(string nombre) : base(nombre) { }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.TieneDestino, false);
            return Status.Success;
        }
    }

    /// <summary>
    /// Espera unos segundos y termina con Success.
    ///
    /// Los segundos llegan como lambda (Func&lt;float&gt;) y no como número fijo.
    /// Así se leen de GuardiaAgente cada vez que el nodo empieza: si cambiáis
    /// TiempoDescanso en el inspector en pleno Play, el árbol lo nota igual que
    /// la FSM.
    ///
    /// Usa bb.Tiempo y no Time.time porque el runtime del árbol no depende de
    /// UnityEngine: el tiempo lo escribe GuardiaAgente en la pizarra.
    /// </summary>
    public class Esperar : Node
    {
        private readonly Func<float> _segundos;
        private float _fin;

        public Esperar(string nombre, Func<float> segundos) : base(nombre)
        {
            _segundos = segundos;
        }

        protected override void OnInitialize(Blackboard bb)
        {
            _fin = bb.Tiempo + _segundos();
        }

        protected override Status Update(Blackboard bb)
        {
            return bb.Tiempo >= _fin ? Status.Success : Status.Running;
        }
    }
}
