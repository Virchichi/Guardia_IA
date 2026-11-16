// ═══════════════════════════════════════════════════════════════════════════
//  AnimadorGuardia.cs — PRESENTACIÓN: traduce lo que pasa a parámetros del Animator
//
//  Los estados (FSM) y las hojas (árbol) NO tocan el Animator. Este
//  componente solo LEE (pizarra, NavMeshAgent y AtaqueDistancia) y escribe
//  parámetros. Por eso las animaciones funcionan igual con los dos cerebros y
//  al cambiar de uno a otro en caliente.
//
//  Parámetros del Animator (los crea Tools > Guardia > Crear Animator):
//    Velocidad   float   m/s reales del NavMeshAgent → blend Idle/Andar/Correr
//    Inquieto    bool    sospecha o alerta, sin verlo y quieto → mirar alrededor
//    Apuntando   bool    la decisión quiere disparar
//    Disparar    trigger sale una bala
//    Recargando  bool    AtaqueDistancia está recargando
//    Muerto      trigger llamar a Morir()
//
//  La capa «Arma» (solo tronco y brazos) se enciende al apuntar o recargar y
//  se apaga suavemente al terminar: así los brazos no se quedan rígidos al
//  andar.
//
//  Va en el MISMO GameObject que GuardiaAgente.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.AI;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(GuardiaAgente))]
    public class AnimadorGuardia : MonoBehaviour
    {
        [Tooltip("Por debajo de esta velocidad (m/s) se considera que está quieto.")]
        public float UmbralQuieto = 0.1f;

        [Tooltip("Suavizado del parámetro Velocidad (más alto = reacciona antes).")]
        public float SuavizadoVelocidad = 10f;

        [Tooltip("Velocidad con la que entra y sale la capa del arma.")]
        public float VelocidadCapaArma = 6f;

        private static readonly int P_Velocidad  = Animator.StringToHash("Velocidad");
        private static readonly int P_Inquieto   = Animator.StringToHash("Inquieto");
        private static readonly int P_Apuntando  = Animator.StringToHash("Apuntando");
        private static readonly int P_Disparar   = Animator.StringToHash("Disparar");
        private static readonly int P_Recargando = Animator.StringToHash("Recargando");
        private static readonly int P_Muerto     = Animator.StringToHash("Muerto");

        private GuardiaAgente _agente;
        private Animator _animator;
        private NavMeshAgent _nav;
        private AtaqueDistancia _arma;
        private int _capaArma = -1;
        private float _velocidad;
        private bool _muerto;

        void Awake()
        {
            _agente = GetComponent<GuardiaAgente>();
            _animator = GetComponentInChildren<Animator>();
            _nav = GetComponent<NavMeshAgent>();
            _arma = GetComponent<AtaqueDistancia>();

            if (_animator != null)
            {
                _animator.applyRootMotion = false;   // quien mueve es el NavMeshAgent
                _capaArma = _animator.GetLayerIndex("Arma");
            }
        }

        void OnEnable()  { if (_arma != null) _arma.AlDisparar += AlDisparar; }
        void OnDisable() { if (_arma != null) _arma.AlDisparar -= AlDisparar; }

        void Update()
        {
            if (_animator == null || _muerto) return;

            // ── Velocidad real, suavizada ───────────────────────────────────
            float real = _nav != null ? _nav.velocity.magnitude : 0f;
            _velocidad = Mathf.Lerp(_velocidad, real, 1f - Mathf.Exp(-SuavizadoVelocidad * Time.deltaTime));
            if (_velocidad < 0.01f) _velocidad = 0f;
            _animator.SetFloat(P_Velocidad, _velocidad);

            // ── Lo que dice la pizarra ──────────────────────────────────────
            bool apuntando = false;
            bool inquieto = false;
            if (_agente != null && _agente.Pizarra != null)
            {
                var bb = _agente.Pizarra;
                NivelSospecha nivel = bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
                bool visible = bb.Get(ClavesBB.Visible, false);

                apuntando = bb.Get(ClavesBB.QuiereDisparar, false);
                inquieto = nivel != NivelSospecha.Tranquilo && !visible && _velocidad < UmbralQuieto;
            }

            bool recargando = _arma != null && _arma.recargando;

            _animator.SetBool(P_Apuntando, apuntando);
            _animator.SetBool(P_Inquieto, inquieto && !apuntando);
            _animator.SetBool(P_Recargando, recargando);

            // ── Capa del arma: solo cuando hace falta ───────────────────────
            if (_capaArma >= 0)
            {
                float objetivo = (apuntando || recargando) ? 1f : 0f;
                float actual = _animator.GetLayerWeight(_capaArma);
                _animator.SetLayerWeight(_capaArma,
                    Mathf.MoveTowards(actual, objetivo, VelocidadCapaArma * Time.deltaTime));
            }
        }

        private void AlDisparar()
        {
            if (_animator != null && !_muerto) _animator.SetTrigger(P_Disparar);
        }

        /// <summary>Para cuando tengáis vida en el guardia: reproduce la muerte y congela el resto.</summary>
        public void Morir()
        {
            if (_animator == null || _muerto) return;
            _muerto = true;
            if (_capaArma >= 0) _animator.SetLayerWeight(_capaArma, 0f);
            _animator.SetTrigger(P_Muerto);
        }
    }
}
