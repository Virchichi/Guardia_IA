// ═══════════════════════════════════════════════════════════════════════════
//  RitmoGuardia.cs — CAPA DE ACCIÓN: anda tranquilo, corre en alerta
//
//  Lee el nivel de la pizarra y ajusta el NavMeshAgent. Así el Animator puede
//  mezclar andar y correr con sentido, y ningún estado ni nodo toca el
//  NavMeshAgent. (LocomocionNavMesh no se modifica.)
//
//  Al correr también sube la aceleración y el giro y quita el frenado
//  automático: persiguiendo, el destino cambia en cada tick y, con los valores
//  de andar, el agente frenaba y aceleraba sin parar. Eso hacía que el
//  Animator mezclara andar/correr continuamente y el cuerpo se balanceara.
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
        [Header("Andar (patrullar, investigar, buscar)")]
        [Tooltip("m/s. Debe coincidir con el umbral de «andar» del blend tree (1,6 por defecto).")]
        public float VelocidadAndar = 1.6f;
        public float AceleracionAndar = 8f;
        public float GiroAndar = 240f;

        [Header("Correr (alerta)")]
        [Tooltip("m/s. Debe coincidir con el umbral de «correr» del blend tree (4 por defecto).")]
        public float VelocidadCorrer = 4f;
        [Tooltip("Alta para que llegue a la velocidad de correr y no oscile entre andar y correr.")]
        public float AceleracionCorrer = 20f;
        [Tooltip("Grados por segundo: girar rápido evita los zigzags al seguir a un objetivo que se mueve.")]
        public float GiroCorrer = 540f;

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

            _nav.speed        = alerta ? VelocidadCorrer   : VelocidadAndar;
            _nav.acceleration = alerta ? AceleracionCorrer : AceleracionAndar;
            _nav.angularSpeed = alerta ? GiroCorrer        : GiroAndar;

            // Persiguiendo, frenar al acercarse al destino (que se mueve) provoca
            // tirones. Andando, sí queremos que frene suave al llegar al waypoint.
            _nav.autoBraking = !alerta;
        }
    }
}
