// ═══════════════════════════════════════════════════════════════════════════
//  GuardiaGizmos.cs — Hacer visible lo que el guardia piensa
//
//  Inteligencia Artificial (IA3) · GDDVV 3º · UDIT · Curso 2026/2027
//  Dr. Luis Peña Sánchez
//
//  MATERIAL DE PARTIDA — terminado, pero AMPLIADLO.
//
//  El requisito A.6 del enunciado dice que el estado interno tiene que ser
//  legible desde fuera. No es un capricho de corrección: es que sin esto no
//  podéis depurar. Un medidor de sospecha que no se ve es un número del que
//  no sabéis si sube demasiado rápido.
//
//  Y es también diseño de juego: si el jugador no percibe que el guardia
//  empieza a sospechar, para él la detección es instantánea e injusta.
//
//  ── AMPLIADO (nuestro) ───────────────────────────────────────────────────
//  · Dos conos: visión DIRECTA (opaco) y PERIFÉRICA (semitransparente).
//  · Radio de PROXIMIDAD (cuerpo a cuerpo) y radio reducido si va agachado.
//  · La línea al jugador cambia de color según qué sistema lo detecta:
//      rojo = directa · cian = periférica · naranja = proximidad.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UDIT.IA3.BT;

namespace UDIT.IA3.Guardia
{
    [RequireComponent(typeof(GuardiaAgente))]
    public class GuardiaGizmos : MonoBehaviour
    {
        public bool DibujarCono = true;
        public bool DibujarPeriferica = true;
        public bool DibujarProximidad = true;
        public bool DibujarSospecha = true;
        public bool DibujarUltimaPos = true;

        private static readonly Color ColorPeriferica = new Color(0.2f, 0.8f, 1f);
        private static readonly Color ColorProximidad = new Color(1f, 0.55f, 0.1f);

        private GuardiaAgente _agente;
        private SensorVision _vision;

        private void Awake()
        {
            _agente = GetComponent<GuardiaAgente>();
            _vision = GetComponent<SensorVision>();
        }

        private static Color ColorDe(NivelSospecha n)
        {
            switch (n)
            {
                case NivelSospecha.Alerta:   return new Color(1f, 0.2f, 0.2f);
                case NivelSospecha.Sospecha: return new Color(1f, 0.85f, 0.2f);
                default:                     return new Color(0.4f, 0.9f, 0.4f);
            }
        }

        private void OnDrawGizmos()
        {
            if (_vision == null) _vision = GetComponent<SensorVision>();
            if (_vision == null) return;

            Transform ojos = _vision.Ojos != null ? _vision.Ojos : transform;
            NivelSospecha nivel = _vision.NivelActual;
            Color c = ColorDe(nivel);

            Vector3 adelante = ojos.forward;
            adelante.y = 0f;
            if (adelante.sqrMagnitude < 0.0001f) adelante = transform.forward;
            adelante.Normalize();

            // ── Visión periférica (debajo, semitransparente) ────────────────
            if (DibujarPeriferica)
            {
                float medioPer = Mathf.Max(_vision.AnguloPeriferico, _vision.AnguloVision) * 0.5f;
                Color cp = ColorPeriferica;
                cp.a = 0.45f;
                Arco(ojos.position, adelante, medioPer, _vision.AlcancePeriferico, cp);
            }

            // ── Visión directa (color según el nivel de sospecha) ───────────
            if (DibujarCono)
            {
                Arco(ojos.position, adelante, _vision.AnguloVision * 0.5f, _vision.Alcance,
                     new Color(c.r, c.g, c.b, 0.85f));
            }

            // ── Proximidad (cuerpo a cuerpo): círculos en el suelo ──────────
            if (DibujarProximidad && _vision.UsarProximidad)
            {
                Color cx = ColorProximidad;
                cx.a = (Application.isPlaying && _vision.CercaAhora) ? 1f : 0.6f;
                Circulo(transform.position + Vector3.up * 0.05f, _vision.RadioProximidad, cx);

                if (_vision.MultiplicadorRadioAgachado < 1f)
                {
                    cx.a *= 0.5f;
                    Circulo(transform.position + Vector3.up * 0.05f,
                            _vision.RadioProximidad * _vision.MultiplicadorRadioAgachado, cx);
                }
            }

            // ── Línea al objetivo: color según QUÉ sistema lo detecta ───────
            if (Application.isPlaying && _vision.Objetivo != null)
            {
                if (_vision.VisibleAhora)
                {
                    Gizmos.color = _vision.EnPerifericaAhora ? ColorPeriferica : Color.red;
                    Gizmos.DrawLine(ojos.position, _vision.Objetivo.position);
                }
                else if (_vision.CercaAhora)
                {
                    Gizmos.color = ColorProximidad;
                    Gizmos.DrawLine(ojos.position, _vision.Objetivo.position);
                }
            }

            // ── La barra de sospecha, sobre la cabeza ───────────────────────
            if (DibujarSospecha && Application.isPlaying)
            {
                Vector3 baseBarra = transform.position + Vector3.up * 2.3f - Vector3.right * 0.5f;
                float s = Mathf.Clamp01(_vision.SospechaActual);

                Gizmos.color = new Color(0f, 0f, 0f, 0.5f);
                Gizmos.DrawLine(baseBarra, baseBarra + Vector3.right * 1f);

                Gizmos.color = c;
                if (s > 0.001f)
                    Gizmos.DrawLine(baseBarra, baseBarra + Vector3.right * s);
            }

            // ── La última posición conocida ─────────────────────────────────
            if (DibujarUltimaPos && Application.isPlaying && _agente != null &&
                _agente.Pizarra != null && _agente.Pizarra.Get(ClavesBB.TieneUltimaPos, false))
            {
                Vector3 lkp = _agente.Pizarra.Get(ClavesBB.UltimaPos, Vector3.zero);

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
                Gizmos.DrawWireCube(lkp + Vector3.up * 0.1f, new Vector3(0.6f, 0.2f, 0.6f));
                Gizmos.DrawLine(transform.position + Vector3.up, lkp + Vector3.up * 0.2f);
            }
        }

        // Cono en el plano horizontal: dos lados y el arco a trozos.
        private static void Arco(Vector3 origen, Vector3 adelante, float medio, float alcance, Color color)
        {
            Gizmos.color = color;
            const int pasos = 24;
            Vector3 previo = origen + Quaternion.Euler(0f, -medio, 0f) * adelante * alcance;
            Gizmos.DrawLine(origen, previo);
            for (int i = 1; i <= pasos; i++)
            {
                float t = Mathf.Lerp(-medio, medio, i / (float)pasos);
                Vector3 p = origen + Quaternion.Euler(0f, t, 0f) * adelante * alcance;
                Gizmos.DrawLine(previo, p);
                previo = p;
            }
            Gizmos.DrawLine(origen, previo);
        }

        private static void Circulo(Vector3 centro, float radio, Color color)
        {
            Gizmos.color = color;
            const int pasos = 32;
            Vector3 previo = centro + Vector3.forward * radio;
            for (int i = 1; i <= pasos; i++)
            {
                float a = i * Mathf.PI * 2f / pasos;
                Vector3 p = centro + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radio;
                Gizmos.DrawLine(previo, p);
                previo = p;
            }
        }
    }
}
