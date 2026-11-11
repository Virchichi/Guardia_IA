// ═══════════════════════════════════════════════════════════════════════════
//  Seguir.cs — Ir detrás de una posición que se mueve, sin terminar nunca
//
//  Se usa en dos ramas:
//    · Perseguir  → seguir al jugador hasta alcanzarlo (distanciaParada = 0)
//    · Investigar → acercarse «sin comprometerse»: se para a cierta distancia
//
//  Devuelve SIEMPRE Running: seguir no «termina» por sí solo. Quien lo corta
//  es el Monitor que lo envuelve, en cuanto su guarda deja de cumplirse (por
//  ejemplo, cuando el guardia deja de ver al jugador).
// ═══════════════════════════════════════════════════════════════════════════

using System;
using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    public class Seguir : NodoMovimiento
    {
        private readonly Func<Blackboard, Vector3> _objetivo;
        private readonly Transform _yo;
        private readonly float _distanciaParada;

        /// <param name="objetivo">Lambda que dice A DÓNDE ir. Se evalúa en cada
        /// tick, así que si la posición cambia, el guardia la sigue.</param>
        /// <param name="yo">Transform del propio guardia. Solo hace falta si
        /// distanciaParada &gt; 0. Leer tu propia posición no es «percibir»: no
        /// mira al jugador ni lanza rayos.</param>
        /// <param name="distanciaParada">A qué distancia del objetivo se para.</param>
        public Seguir(string nombre, Func<Blackboard, Vector3> objetivo,
                      Transform yo = null, float distanciaParada = 0f)
            : base(nombre)
        {
            _objetivo = objetivo;
            _yo = yo;
            _distanciaParada = distanciaParada;
        }

        protected override Status Update(Blackboard bb)
        {
            Vector3 objetivo = _objetivo(bb);

            // Sin distancia de parada (o sin saber dónde estoy): voy directo.
            if (_distanciaParada <= 0f || _yo == null)
            {
                PedirDestino(bb, objetivo);
                return Status.Running;
            }

            // Con distancia de parada: me quedo a _distanciaParada del objetivo.
            Vector3 haciaObjetivo = objetivo - _yo.position;
            haciaObjetivo.y = 0f;                                   // solo el plano del suelo

            if (haciaObjetivo.magnitude <= _distanciaParada)
            {
                Detenerse(bb);                                      // ya estoy lo bastante cerca
            }
            else
            {
                Vector3 puntoCercano = objetivo - haciaObjetivo.normalized * _distanciaParada;
                PedirDestino(bb, puntoCercano);
            }

            return Status.Running;
        }
    }
}
