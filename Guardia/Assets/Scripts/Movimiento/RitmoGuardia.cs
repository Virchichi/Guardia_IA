// ═══════════════════════════════════════════════════════════════════════════
//  RitmoGuardia.cs — CAPA DE ACCIÓN: anda tranquilo, corre en alerta
//
//  Lee el nivel de la pizarra y ajusta la velocidad del NavMeshAgent. Así el
//  Animator puede mezclar andar y correr con sentido, y ningún estado ni nodo
//  toca el NavMeshAgent. (LocomocionNavMesh no se modifica.)
//
//  Va en el MISMO GameObject que GuardiaAgente y el NavMeshAgent.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.AI;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(GuardiaAgente), typeof(NavMeshAgent))]
    public class RitmoGuardia : MonoBehaviour
    {
        [Tooltip("m/s patrullando, investigando o buscando. Debe coincidir con el umbral de " +
                 "«andar» del blend tree (1,6 por defecto).")]
        public float VelocidadAndar = 1.6f;

        [Tooltip("m/s en alerta (persiguiendo). Debe coincidir con el umbral de «correr» (4 por defecto).")]
        public float VelocidadCorrer = 4f;

        private GuardiaAgente _agente;
        private NavMeshAgent _nav;

        void Awake()
        {
            _agente = GetComponent<GuardiaAgente>();
            _nav = GetComponent<NavMeshAgent>();
        }

        void Update()
        {
            if (_agente.Pizarra == null) return;

            bool alerta = _agente.Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo) == NivelSospecha.Alerta;
            _nav.speed = alerta ? VelocidadCorrer : VelocidadAndar;
        }
    }
}
