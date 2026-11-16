// ═══════════════════════════════════════════════════════════════════════════
//  AnimadorJugador.cs — Traduce el estado del jugador a parámetros del Animator
//
//  Parámetros (los crea Tools > Jugador > Crear jugador de sigilo):
//    Velocidad  float    m/s horizontales reales
//    Agachado   float    0 de pie … 1 agachado (mezcla suave entre los dos blend)
//    Giro       float    −1 … 1, giro en el sitio (izquierda/derecha) estando quieto
//    Agacharse  trigger  se agacha estando quieto  → «Standing To Crouched»
//    Levantarse trigger  se levanta estando quieto → «Crouched To Standing»
//    Golpe      trigger  le ha dado una bala
//    Muerto     trigger  sin vida
// ═══════════════════════════════════════════════════════════════════════════

using System.Collections.Generic;
using UnityEngine;

namespace UDIT.IA3.Jugador
{
    [RequireComponent(typeof(JugadorSigilo))]
    public class AnimadorJugador : MonoBehaviour
    {
        [Tooltip("Suavizado del parámetro Velocidad (segundos).")]
        public float SuavizadoVelocidad = 0.1f;
        [Tooltip("Grados por segundo de giro que corresponden a Giro = 1.")]
        public float GiroMaximo = 200f;
        [Tooltip("Por debajo de esta velocidad (m/s), agacharse/levantarse usa su animación de transición.")]
        public float UmbralTransicion = 0.3f;

        private static readonly int P_Velocidad = Animator.StringToHash("Velocidad");
        private static readonly int P_Agachado  = Animator.StringToHash("Agachado");
        private static readonly int P_Golpe     = Animator.StringToHash("Golpe");
        private static readonly int P_Muerto    = Animator.StringToHash("Muerto");
        private static readonly int P_Giro      = Animator.StringToHash("Giro");
        private static readonly int P_Agacharse = Animator.StringToHash("Agacharse");
        private static readonly int P_Levantarse = Animator.StringToHash("Levantarse");

        private JugadorSigilo _jugador;
        private Animator _animator;
        private bool _agachadoPrevio;
        private readonly HashSet<int> _parametros = new HashSet<int>();

        void Awake()
        {
            _jugador = GetComponent<JugadorSigilo>();
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator != null)
            {
                _animator.applyRootMotion = false;   // mueve el CharacterController

                // Solo tocamos los parámetros que existan: si el controller está
                // desactualizado, no llenamos la consola de avisos.
                foreach (var p in _animator.parameters) _parametros.Add(p.nameHash);
                if (!_parametros.Contains(P_Giro))
                    Debug.LogWarning("[AnimadorJugador] El Animator no tiene los parámetros nuevos. " +
                                     "Ejecutad Tools > Jugador > Crear jugador de sigilo.", this);
            }
        }

        bool Tiene(int hash) => _parametros.Contains(hash);

        void Update()
        {
            if (_animator == null) return;
            if (Tiene(P_Velocidad)) _animator.SetFloat(P_Velocidad, _jugador.VelocidadActual, SuavizadoVelocidad, Time.deltaTime);
            if (Tiene(P_Agachado))  _animator.SetFloat(P_Agachado, _jugador.CantidadAgachado);

            // Giro en el sitio (solo se ve estando quieto: está en la parte «parado» del blend)
            float giro = Mathf.Clamp(_jugador.GiroActual / GiroMaximo, -1f, 1f);
            if (Tiene(P_Giro)) _animator.SetFloat(P_Giro, giro, 0.1f, Time.deltaTime);

            // Transiciones de agacharse/levantarse estando quieto.
            bool quieto = _jugador.VelocidadActual < UmbralTransicion;
            bool hayTransiciones = Tiene(P_Agacharse) && Tiene(P_Levantarse);
            if (!hayTransiciones)
            {
                _agachadoPrevio = _jugador.Agachado;
            }
            else if (_jugador.Agachado != _agachadoPrevio)
            {
                _animator.ResetTrigger(P_Agacharse);
                _animator.ResetTrigger(P_Levantarse);
                if (quieto) _animator.SetTrigger(_jugador.Agachado ? P_Agacharse : P_Levantarse);
                _agachadoPrevio = _jugador.Agachado;
            }
            else if (!quieto)
            {
                // Si echa a andar, que no quede un trigger pendiente que salte luego.
                _animator.ResetTrigger(P_Agacharse);
                _animator.ResetTrigger(P_Levantarse);
            }
        }

        public void Golpe()  { if (_animator != null && Tiene(P_Golpe))  _animator.SetTrigger(P_Golpe); }
        public void Morir()  { if (_animator != null && Tiene(P_Muerto)) _animator.SetTrigger(P_Muerto); }

        /// <summary>Vuelve al estado inicial del Animator (al reaparecer).</summary>
        public void Reiniciar()
        {
            if (_animator == null) return;
            _animator.Rebind();
            _animator.Update(0f);
            _agachadoPrevio = false;
        }
    }
}
