// ═══════════════════════════════════════════════════════════════════════════
//  IndicadorSospecha.cs — PRESENTACIÓN: el estado del guardia visible en el juego
//
//  Los gizmos solo se ven en la vista Scene. Este componente dibuja sobre la
//  cabeza del guardia, en la vista Game:
//    · una barra con la sospecha continua (0..1),
//    · «?» amarillo en Sospecha y «!» rojo en Alerta,
//    · opcionalmente, qué está haciendo el cerebro (la traza).
//
//  Solo LEE la pizarra: no decide nada. Apartado A.6 (legibilidad).
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(GuardiaAgente))]
    public class IndicadorSospecha : MonoBehaviour
    {
        [Tooltip("Metros sobre el pivote del guardia donde se dibuja el indicador.")]
        public float Altura = 1.3f;
        public bool MostrarBarra = true;
        [Tooltip("Muestra debajo qué cerebro usa y en qué estado/rama está. Útil para el vídeo.")]
        public bool MostrarCerebro = true;

        private GuardiaAgente _agente;
        private GUIStyle _simbolo, _texto;
        private static Texture2D _blanco;

        void Awake()
        {
            _agente = GetComponent<GuardiaAgente>();
        }

        void OnGUI()
        {
            Camera cam = Camera.main;
            if (cam == null || _agente == null || _agente.Pizarra == null) return;

            Vector3 pantalla = cam.WorldToScreenPoint(transform.position + Vector3.up * Altura);
            if (pantalla.z <= 0f) return;                       // detrás de la cámara

            float x = pantalla.x;
            float y = Screen.height - pantalla.y;                // OnGUI tiene la Y invertida

            if (_simbolo == null)
            {
                _simbolo = new GUIStyle(GUI.skin.label)
                    { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _texto = new GUIStyle(GUI.skin.label)
                    { fontSize = 11, alignment = TextAnchor.MiddleCenter };
                _blanco = Texture2D.whiteTexture;
            }

            var bb = _agente.Pizarra;
            float sospecha = bb.Get(ClavesBB.Sospecha, 0f);
            NivelSospecha nivel = bb.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);

            Color color = nivel == NivelSospecha.Alerta   ? new Color(0.95f, 0.2f, 0.2f)
                        : nivel == NivelSospecha.Sospecha ? new Color(1f, 0.8f, 0.1f)
                        :                                   new Color(0.6f, 0.9f, 0.6f);

            // ── Símbolo ─────────────────────────────────────────────────────
            if (nivel != NivelSospecha.Tranquilo)
            {
                string s = nivel == NivelSospecha.Alerta ? "!" : "?";
                Sombra(new Rect(x - 20, y - 52, 40, 36), s, _simbolo, color);
            }

            // ── Barra de sospecha ───────────────────────────────────────────
            if (MostrarBarra && sospecha > 0.005f)
            {
                const float ancho = 60f, alto = 6f;
                Rect fondo = new Rect(x - ancho / 2, y - 12, ancho, alto);
                Dibujar(new Rect(fondo.x - 1, fondo.y - 1, fondo.width + 2, fondo.height + 2), new Color(0, 0, 0, 0.7f));
                Dibujar(new Rect(fondo.x, fondo.y, ancho * Mathf.Clamp01(sospecha), alto), color);
            }

            // ── Cerebro y estado ────────────────────────────────────────────
            if (MostrarCerebro)
            {
                string traza = _agente.Cerebro == TipoDecisor.MaquinaDeEstados ? "FSM" : "Árbol";
                Sombra(new Rect(x - 90, y - 4, 180, 18), traza, _texto, Color.white);
            }
        }

        private static void Dibujar(Rect r, Color c)
        {
            Color antes = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, _blanco);
            GUI.color = antes;
        }

        private static void Sombra(Rect r, string texto, GUIStyle estilo, Color color)
        {
            Color antes = GUI.color;
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.Label(new Rect(r.x + 1, r.y + 1, r.width, r.height), texto, estilo);
            GUI.color = color;
            GUI.Label(r, texto, estilo);
            GUI.color = antes;
        }
    }
}
