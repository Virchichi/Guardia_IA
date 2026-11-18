// ═══════════════════════════════════════════════════════════════════════════
//  NodoMovimiento.cs — Base común de los nodos hoja que PIDEN un destino
//
//  Regla 2 del enunciado: «la decisión no mueve». Estos nodos solo escriben
//  en la pizarra «quiero ir a X» (Destino + TieneDestino). Quien llama a
//  NavMeshAgent.SetDestination es LocomocionNavMesh, y nadie más.
//
//  Sigue el patrón del IrA de los apuntes (B3.3a, diapositiva 6):
//
//    · Al pedir un destino se pone HaLlegado = false. Si no, en el primer
//      tick seguiría valiendo la llegada del destino ANTERIOR y la acción
//      terminaría en el acto, sin haberse movido.
//
//    · OnTerminate(Aborted) RETIRA la petición (TieneDestino = false). Si el
//      Monitor o el ActiveSelector cortan la acción y no la retiramos, el
//      guardia sigue andando hacia el destino viejo.
//
//  Todos los nodos que mueven heredan de aquí: IrA, Seguir y
//  AcercarseConCautela. Así esas dos reglas se escriben una sola vez.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public abstract class NodoMovimiento : Node
    {
        protected NodoMovimiento(string nombre) : base(nombre) { }

        /// <summary>Escribe en la pizarra «quiero ir a destino». NO mueve.</summary>
        protected static void PedirDestino(Blackboard bb, Vector3 destino)
        {
            bb.Set(ClavesBB.Destino, destino);
            bb.Set(ClavesBB.TieneDestino, true);
            bb.Set(ClavesBB.HaLlegado, false);
        }

        /// <summary>
        /// Si nos cortan a mitad de camino, retiramos la petición. Las clases
        /// hijas que sobrescriban OnTerminate deben llamar a base.OnTerminate.
        /// </summary>
        protected override void OnTerminate(Blackboard bb, Status resultado)
        {
            if (resultado == Status.Aborted)
                bb.Set(ClavesBB.TieneDestino, false);
        }
    }
}
