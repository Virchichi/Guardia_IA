// ═══════════════════════════════════════════════════════════════════════════
//  IrA.cs — Ir a un punto y TERMINAR al llegar
//
//  Es el IrA de los apuntes (B3.3a, diapositiva 6), con una diferencia: el
//  destino no sale de una clave fija, sino de una lambda. Así el mismo nodo
//  sirve para ir al waypoint de la ruta, a la última posición conocida o a un
//  punto de búsqueda.
//
//    OnInitialize → pide el destino (una sola vez: el punto no cambia)
//    Update       → Running mientras va, Success al llegar
//    OnTerminate  → si lo abortan, retira la petición (NodoMovimiento)
//
//  Como termina con Success, puede ir dentro de una Sequence: «ve allí y
//  DESPUÉS haz otra cosa».
// ═══════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class IrA : NodoMovimiento
    {
        private readonly Func<Blackboard, Vector3> _destino;

        /// <param name="destino">Lambda que dice a dónde ir. Se evalúa UNA vez,
        /// al empezar. Ejemplo: bb =&gt; bb.Get(ClavesBB.UltimaPos, Vector3.zero)</param>
        public IrA(string nombre, Func<Blackboard, Vector3> destino) : base(nombre)
        {
            _destino = destino;
        }

        protected override void OnInitialize(Blackboard bb)
        {
            PedirDestino(bb, _destino(bb));
        }

        protected override Status Update(Blackboard bb)
        {
            return bb.Get(ClavesBB.HaLlegado, false) ? Status.Success : Status.Running;
        }
    }
}
