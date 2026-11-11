// ═══════════════════════════════════════════════════════════════════════════
//  AccionesBusqueda.cs — Nodos hoja pequeños que forman la rama de búsqueda
//
//  La búsqueda NO es un nodo gigante: se monta en DecisorBT combinando piezas
//  pequeñas con una Sequence y un Repeat:
//
//    Sequence "Busqueda"
//      ├── IrA (última posición conocida)
//      ├── Repeat x N
//      │     └── Sequence
//      │           ├── ElegirPuntoDeBusqueda   ← este fichero
//      │           ├── IrA (punto elegido)
//      │           └── Esperar                 ← este fichero
//      ├── Esperar
//      └── OlvidarUltimaPos                    ← este fichero
//
//  Esa es la gracia de un árbol: piezas pequeñas y reutilizables (IrA y
//  Esperar aparecen varias veces) que se combinan.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    /// <summary>
    /// Elige un punto al azar alrededor de la última posición conocida y lo
    /// apunta en la pizarra (ClavesBB.PuntoBusqueda) para que un IrA vaya allí.
    /// Termina en el mismo tick: siempre Success.
    ///
    /// ¿Por qué no elegir el punto dentro de la lambda del IrA? Porque esa lambda
    /// se evalúa en CADA tick: elegiría un punto distinto cada décima de segundo
    /// y el guardia daría tumbos. Elegir y luego ir son dos pasos separados.
    /// </summary>
    public class ElegirPuntoDeBusqueda : Node
    {
        private readonly float _radio;

        public ElegirPuntoDeBusqueda(string nombre, float radio) : base(nombre)
        {
            _radio = radio;
        }

        protected override Status Update(Blackboard bb)
        {
            Vector3 centro = bb.Get(ClavesBB.UltimaPos, Vector3.zero);
            Vector2 azar = Random.insideUnitCircle * _radio;

            // Si el punto cae dentro de una pared, el NavMeshAgent irá al punto
            // alcanzable más cercano. No hace falta comprobarlo aquí (y no
            // debemos: eso sería trabajo de la capa de movimiento).
            bb.Set(ClavesBB.PuntoBusqueda, centro + new Vector3(azar.x, 0f, azar.y));
            return Status.Success;
        }
    }

    /// <summary>
    /// Espera <c>segundos</c> y termina con Success. Es el «mirar alrededor» del
    /// guardia: se queda quieto un momento en cada punto.
    ///
    /// Usa bb.Tiempo y no Time.time porque el runtime del árbol no depende de
    /// UnityEngine: el tiempo lo escribe GuardiaAgente en la pizarra.
    /// </summary>
    public class Esperar : Node
    {
        private readonly float _segundos;
        private float _fin;

        public Esperar(string nombre, float segundos) : base(nombre)
        {
            _segundos = segundos;
        }

        protected override void OnInitialize(Blackboard bb)
        {
            _fin = bb.Tiempo + _segundos;   // se recalcula cada vez que el nodo empieza
        }

        protected override Status Update(Blackboard bb)
        {
            return bb.Tiempo >= _fin ? Status.Success : Status.Running;
        }
    }

    /// <summary>
    /// Borra el recuerdo de la última posición: «he buscado y no hay nadie».
    ///
    /// Según la tabla del enunciado, tieneUltimaPos la escriben el «sensor /
    /// acción de buscar» y caduca «al llegar y no ver nada». Este nodo es esa
    /// acción de buscar. Sin él, el guardia recordaría la posición para siempre
    /// (error típico: «sin caducidad»).
    /// </summary>
    public class OlvidarUltimaPos : Node
    {
        public OlvidarUltimaPos(string nombre) : base(nombre) { }

        protected override Status Update(Blackboard bb)
        {
            bb.Set(ClavesBB.TieneUltimaPos, false);
            return Status.Success;
        }
    }
}
