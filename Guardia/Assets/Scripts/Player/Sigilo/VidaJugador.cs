// ═══════════════════════════════════════════════════════════════════════════
//  VidaJugador.cs — Impactos de bala, muerte y reaparición
//
//  Bala.cs llama a RecibirImpacto() al dar al jugador. Con la última vida
//  muere, espera TiempoReaparicion y reaparece donde empezó la partida.
//  Muestra la vida en pantalla con un indicador sencillo.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Jugador
{
    [RequireComponent(typeof(JugadorSigilo))]
    public class VidaJugador : MonoBehaviour
    {
        public int VidaMaxima = 3;
        [Tooltip("Segundos tras morir antes de reaparecer.")]
        public float TiempoReaparicion = 3f;
        [Tooltip("Segundos de invulnerabilidad tras un impacto (evita perder toda la vida en una ráfaga).")]
        public float Invulnerabilidad = 0.6f;
        public bool MostrarEnPantalla = true;

        public int Vida { get; private set; }
        public bool Muerto { get; private set; }

        private JugadorSigilo _jugador;
        private AnimadorJugador _animador;
        private Vector3 _posInicial;
        private Quaternion _rotInicial;
        private float _invulnerableHasta;

        void Awake()
        {
            _jugador = GetComponent<JugadorSigilo>();
            _animador = GetComponent<AnimadorJugador>();
            _posInicial = transform.position;
            _rotInicial = transform.rotation;
            Vida = VidaMaxima;
        }

        public void RecibirImpacto()
        {
            if (Muerto || Time.time < _invulnerableHasta) return;

            Vida--;
            _invulnerableHasta = Time.time + Invulnerabilidad;

            if (Vida <= 0) Morir();
            else if (_animador != null) _animador.Golpe();
        }

        private void Morir()
        {
            Muerto = true;
            _jugador.Control = false;
            if (_animador != null) _animador.Morir();
            Invoke(nameof(Reaparecer), TiempoReaparicion);
        }

        private void Reaparecer()
        {
            _jugador.Teletransportar(_posInicial, _rotInicial);
            if (_animador != null) _animador.Reiniciar();
            Vida = VidaMaxima;
            Muerto = false;
            _jugador.Control = true;
        }

        void OnGUI()
        {
            if (!MostrarEnPantalla) return;

            var estilo = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            string corazones = new string('■', Mathf.Max(0, Vida)) + new string('□', Mathf.Max(0, VidaMaxima - Vida));
            GUI.Label(new Rect(20, 16, 400, 34), "Vida  " + corazones, estilo);

            if (_jugador != null && _jugador.Agachado)
                GUI.Label(new Rect(20, 48, 400, 30), "Agachado", estilo);

            if (Muerto)
            {
                var grande = new GUIStyle(estilo) { fontSize = 40, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60), "TE HAN ABATIDO", grande);
            }
        }
    }
}
