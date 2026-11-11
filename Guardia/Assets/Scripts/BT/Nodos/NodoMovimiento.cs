// ═══════════════════════════════════════════════════════════════════════════
//  NodoMovimiento.cs — Base común de los nodos hoja que piden un destino
//
//  Todos los nodos que mueven al guardia hacen lo mismo: escribir «quiero ir
//  a X» en la pizarra (Destino + TieneDestino) y, a veces, preguntar si ya han
//  llegado (HaLlegado, que escribe LocomocionNavMesh). Esta clase reúne esa
//  lógica para no repetirla en cada nodo.
//
//  ¿Por qué hace falta? Por un detalle de tiempos:
//
//    1. El nodo escribe un destino NUEVO en la pizarra.
//    2. En ese mismo frame, la locomoción llama a SetDestination, pero el
//       valor de HaLlegado que hay en la pizarra todavía se calculó para el
//       destino ANTERIOR.
//    3. Si el nodo mirase HaLlegado en el siguiente tick sin más, podría creer
//       que ya ha llegado a un sitio al que ni siquiera ha empezado a ir.
//
//  Solución: cuando pedimos un destino distinto, ignoramos HaLlegado durante
//  un tick, para dar tiempo a que la locomoción responda.
//
//  Ojo: _esperandoRespuesta NO es «en qué estado estoy» (eso sería una FSM
//  disfrazada). Es solo un detalle técnico de sincronización con la capa de
//  movimiento, y se reinicia cada vez que el nodo empieza.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public abstract class NodoMovimiento : Node
    {
        /// <summary>Distancia mínima para considerar que el destino ha cambiado.
        /// Es la misma que usa LocomocionNavMesh para decidir si recalcula ruta.</summary>
        private const float CambioMinimoCuadrado = 0.01f;

        private bool _esperandoRespuesta;

        protected NodoMovimiento(string nombre) : base(nombre) { }

        protected override void OnInitialize(Blackboard bb)
        {
            _esperandoRespuesta = false;
        }

        /// <summary>
        /// Escribe en la pizarra «quiero ir a destino». NO mueve: quien mueve es
        /// LocomocionNavMesh, que lee esta clave en la fase Actuar.
        /// </summary>
        protected void PedirDestino(Blackboard bb, Vector3 destino)
        {
            bool yaTeniaDestino = bb.Get(ClavesBB.TieneDestino, false);
            Vector3 anterior = bb.Get(ClavesBB.Destino, destino);

            bool esNuevo = !yaTeniaDestino ||
                           (anterior - destino).sqrMagnitude > CambioMinimoCuadrado;

            if (esNuevo)
            {
                bb.Set(ClavesBB.Destino, destino);
                bb.Set(ClavesBB.TieneDestino, true);
                _esperandoRespuesta = true;
            }
        }

        /// <summary>Pide a la locomoción que se quede quieto.</summary>
        protected void Detenerse(Blackboard bb)
        {
            bb.Set(ClavesBB.TieneDestino, false);
        }

        /// <summary>
        /// ¿Ha llegado al último destino que pidió ESTE nodo? Si acaba de pedir
        /// uno nuevo, responde «no» durante un tick (ver cabecera del fichero).
        /// </summary>
        protected bool HaLlegado(Blackboard bb)
        {
            if (_esperandoRespuesta)
            {
                _esperandoRespuesta = false;
                return false;
            }
            return bb.Get(ClavesBB.HaLlegado, false);
        }
    }
}
