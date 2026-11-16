// ═══════════════════════════════════════════════════════════════════════════
//  JugadorSigilo.cs — Movimiento del jugador pensado para sigilo
//
//  Tres ritmos, cada uno con su coste:
//    · Agachado (C, toggle)        — lento, cápsula baja, se le ve mucho menos.
//    · Sigilo de pie (por defecto) — andar cauteloso, poco llamativo.
//    · Correr (Shift)              — rápido, pero llama la atención (el sensor
//                                    del guardia mide la velocidad del objetivo).
//
//  Giro en el sitio: si estás quieto y pides ir hacia un lado muy distinto al
//  que miras, primero se gira (con su animación) y luego echa a andar.
//
//  Movimiento relativo a la cámara (CameraController), con aceleración y
//  giro suaves para que se sienta pesado y controlable, no «de parkour».
//  No hay salto: en un juego de sigilo resolvía los problemas sin esconderse.
//
//  Implementa IObjetivoSigilo: el sensor de visión del guardia usa su altura
//  real y su postura.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.Guardia;

namespace UDIT.IA3.Jugador
{
    [RequireComponent(typeof(CharacterController))]
    public class JugadorSigilo : MonoBehaviour, IObjetivoSigilo
    {
        [Header("Velocidades (m/s)")]
        public float VelocidadAgachado = 1.3f;
        [Tooltip("Andar sigiloso de pie: el ritmo por defecto.")]
        public float VelocidadSigilo = 1.6f;
        public float VelocidadCorrer = 4.8f;
        [Tooltip("m/s² para acelerar y frenar. Más bajo = más inercia.")]
        public float Aceleracion = 12f;
        [Tooltip("Grados por segundo al girar hacia donde se mueve.")]
        public float VelocidadGiro = 600f;

        [Header("Giro en el sitio")]
        [Tooltip("Parado, si la dirección pedida se desvía más de estos grados, primero gira sin avanzar.")]
        public float AnguloGiroEnSitio = 70f;
        [Tooltip("Grados por segundo del giro en el sitio (más lento = se ve la animación).")]
        public float VelocidadGiroEnSitio = 300f;
        [Tooltip("Por debajo de esta velocidad (m/s) se considera parado para girar en el sitio.")]
        public float UmbralParado = 0.6f;

        [Header("Agacharse")]
        public float AlturaAgachado = 1.1f;
        [Tooltip("Metros por segundo que cambia la altura de la cápsula al agacharse o levantarse.")]
        public float VelocidadCambioAltura = 5f;
        [Tooltip("Multiplicador de la evidencia del guardia estando agachado (1 = igual que de pie).")]
        [Range(0.1f, 1f)] public float FactorPosturaAgachado = 0.55f;
        [Tooltip("Desactivado: C alterna agachado/de pie. Activado: hay que mantener C.")]
        public bool MantenerParaAgacharse = false;

        [Header("Gravedad")]
        public float Gravedad = -20f;

        [Header("Referencias")]
        [Tooltip("Si se deja vacío, se busca en la escena.")]
        public CameraController Camara;

        // ── Estado público (lo leen AnimadorJugador y el HUD) ──────────────
        public bool Agachado { get; private set; }
        public bool Corriendo { get; private set; }
        /// <summary>Velocidad horizontal real en m/s.</summary>
        public float VelocidadActual { get; private set; }
        /// <summary>0 de pie … 1 agachado del todo (suave, para el Animator).</summary>
        public float CantidadAgachado { get; private set; }
        /// <summary>Grados por segundo que está girando ahora (+ derecha, − izquierda).</summary>
        public float GiroActual { get; private set; }
        /// <summary>Está girando parado antes de echar a andar.</summary>
        public bool GirandoEnSitio { get; private set; }
        /// <summary>Si es false, el jugador no responde al input (p. ej. muerto).</summary>
        public bool Control { get; set; } = true;

        // ── IObjetivoSigilo ────────────────────────────────────────────────
        public float AlturaVisible => _cc != null ? _cc.height : 1.75f;
        public float FactorPostura => Mathf.Lerp(1f, FactorPosturaAgachado, CantidadAgachado);

        private CharacterController _cc;
        private InputSystem_Actions _input;
        private float _alturaDePie;
        private float _desfaseCentro;
        private Vector3 _velPlano;
        private float _velVertical;
        private bool _quiereAgacharse;
        private float _yawPrevio;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            _alturaDePie = _cc.height;
            _desfaseCentro = _cc.center.y - _cc.height * 0.5f;
            _yawPrevio = transform.eulerAngles.y;

            if (Camara == null) Camara = FindAnyObjectByType<CameraController>();
        }

        void OnEnable()
        {
            if (_input == null) _input = new InputSystem_Actions();
            _input.Player.Enable();
        }

        void OnDisable()
        {
            if (_input != null) _input.Player.Disable();
        }

        void Update()
        {
            float dt = Time.deltaTime;

            Vector2 mover = Vector2.zero;
            bool sprint = false;

            if (Control)
            {
                mover = Vector2.ClampMagnitude(_input.Player.Move.ReadValue<Vector2>(), 1f);
                sprint = _input.Player.Sprint.IsPressed();

                if (MantenerParaAgacharse)
                    _quiereAgacharse = _input.Player.Crouch.IsPressed();
                else if (_input.Player.Crouch.WasPressedThisFrame())
                    _quiereAgacharse = !_quiereAgacharse;

                // Echar a correr te levanta.
                if (sprint && mover.sqrMagnitude > 0.01f) _quiereAgacharse = false;
            }

            // Si quiere levantarse pero hay algo encima, sigue agachado.
            Agachado = _quiereAgacharse || !PuedeLevantarse();
            Corriendo = sprint && !Agachado && mover.sqrMagnitude > 0.01f;

            // ── Velocidad horizontal con aceleración ───────────────────────
            float maxima = Agachado ? VelocidadAgachado : (Corriendo ? VelocidadCorrer : VelocidadSigilo);

            Vector3 dir = new Vector3(mover.x, 0f, mover.y);
            if (Camara != null) dir = Camara.PlanarRotation * dir;

            // ¿Giro en el sitio? Parado y pidiendo una dirección muy distinta.
            GirandoEnSitio = dir.sqrMagnitude > 0.0001f &&
                             _velPlano.magnitude < UmbralParado &&
                             Vector3.Angle(transform.forward, dir) > AnguloGiroEnSitio;

            Vector3 objetivo = GirandoEnSitio ? Vector3.zero : dir * maxima;   // dir trae la intensidad del stick
            _velPlano = Vector3.MoveTowards(_velPlano, objetivo, Aceleracion * dt);

            // ── Girar hacia donde se mueve ──────────────────────────────────
            if (dir.sqrMagnitude > 0.0001f)
            {
                float vGiro = GirandoEnSitio ? VelocidadGiroEnSitio : VelocidadGiro;
                Quaternion deseada = Quaternion.LookRotation(dir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, deseada, vGiro * dt);
            }

            float yaw = transform.eulerAngles.y;
            GiroActual = dt > 0f ? Mathf.DeltaAngle(_yawPrevio, yaw) / dt : 0f;
            _yawPrevio = yaw;

            // ── Gravedad ────────────────────────────────────────────────────
            if (_cc.isGrounded && _velVertical < 0f) _velVertical = -2f;
            else                                      _velVertical += Gravedad * dt;

            _cc.Move((_velPlano + Vector3.up * _velVertical) * dt);

            Vector3 real = _cc.velocity;
            real.y = 0f;
            VelocidadActual = real.magnitude;

            // ── Altura de la cápsula ────────────────────────────────────────
            float alturaDeseada = Agachado ? AlturaAgachado : _alturaDePie;
            float h = Mathf.MoveTowards(_cc.height, alturaDeseada, VelocidadCambioAltura * dt);
            _cc.height = h;
            _cc.center = new Vector3(_cc.center.x, h * 0.5f + _desfaseCentro, _cc.center.z);
            CantidadAgachado = Mathf.InverseLerp(_alturaDePie, AlturaAgachado, h);

            if (Camara != null) Camara.SetFOVState(Corriendo, false);
        }

        /// <summary>¿Hay sitio encima para ponerse de pie?</summary>
        private bool PuedeLevantarse()
        {
            if (_cc.height >= _alturaDePie - 0.01f) return true;

            float radio = _cc.radius * 0.9f;
            Vector3 origen = transform.position + Vector3.up * (_cc.height - _cc.radius);
            float distancia = _alturaDePie - _cc.height;

            // El SphereCast empieza dentro de nuestra propia cápsula, y Unity
            // ignora los colliders en los que ya estás al empezar: no nos detecta.
            return !Physics.SphereCast(origen, radio, Vector3.up, out _, distancia,
                                       ~0, QueryTriggerInteraction.Ignore);
        }

        /// <summary>Teletransporta (para reaparecer) parando cualquier inercia.</summary>
        public void Teletransportar(Vector3 posicion, Quaternion rotacion)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(posicion, rotacion);
            _cc.enabled = true;
            _velPlano = Vector3.zero;
            _velVertical = 0f;
            _quiereAgacharse = false;
            _yawPrevio = rotacion.eulerAngles.y;
        }
    }
}
