// ═══════════════════════════════════════════════════════════════════════════
//  Seguir.cs — Perseguir: ir detrás de la última posición conocida, sin parar
//
//  Mientras el guardia ve al jugador, el sensor actualiza ultimaPos en cada
//  frame. Por eso «ir a ultimaPos» en cada tick es ir detrás del jugador.
//
//  Devuelve SIEMPRE Running: perseguir no termina por sí solo. Quien lo corta
//  es el Monitor de combate, en cuanto deja de verlo o el nivel baja de
//  Alerta. Devolver Success aquí sería el error «Success en una acción que
//  dura» de los apuntes: el guardia temblaría.
//
//  Equivale a EstadoPerseguir de la FSM (paso 4, PedirIrAUltimaPos).
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class Seguir : NodoMovimiento
    {
        public Seguir(string nombre) : base(nombre) { }

        protected override Status Update(Blackboard bb)
        {
            if (bb.Get(ClavesBB.TieneUltimaPos, false))
                PedirDestino(bb, bb.Get(ClavesBB.UltimaPos, Vector3.zero));

            return Status.Running;
        }
    }
}
