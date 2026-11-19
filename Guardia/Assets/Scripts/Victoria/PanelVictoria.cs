// ═══════════════════════════════════════════════════════════════════════════
//  PanelVictoria.cs — Lógica de la pantalla de victoria (la UI está en la escena)
//
//  Se muestra cuando el jugador toca un TriggerVictoria Y ningún guardia está
//  en Alerta. Si alguno sí lo está, no hay victoria: aparece el aviso
//  «¡Escóndete!» durante unos segundos.
//
//  Los elementos visuales son objetos de uGUI (Canvas, Image, TextMeshPro,
//  Button). Para crearlos: Tools > Victoria > Crear panel de victoria.
//  Este script solo enciende/apaga esos objetos y pausa el juego.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UDIT.IA3.Guardia;

namespace UDIT.IA3.Victoria
{
    public class PanelVictoria : MonoBehaviour
    {
        /// <summary>El panel activo de la escena (solo debe haber uno).</summary>
        public static PanelVictoria Instancia { get; private set; }

        [Header("Elementos de UI")]
        [Tooltip("Objeto con el fondo y la caja de victoria. Empieza desactivado.")]
        public GameObject Victoria;
        [Tooltip("Texto de aviso «¡Escóndete!». Empieza desactivado.")]
        public GameObject Aviso;
        public Button BotonReiniciar;
        public Button BotonSalir;

        [Header("Valores")]
        [Tooltip("Segundos que se muestra el aviso cuando hay un guardia en alerta.")]
        public float DuracionAviso = 3f;

        public bool Visible { get; private set; }

        private float _avisoHasta;

        void Awake()
        {
            Instancia = this;

            Victoria.SetActive(false);
            Aviso.SetActive(false);

            BotonReiniciar.onClick.AddListener(Reiniciar);
            BotonSalir.onClick.AddListener(Salir);
        }

        void OnDestroy()
        {
            if (Instancia == this) Instancia = null;
        }

        void Update()
        {
            // Usa tiempo sin escalar: funciona aunque el juego esté pausado.
            if (!Visible && Aviso.activeSelf && Time.unscaledTime >= _avisoHasta)
                Aviso.SetActive(false);
        }

        /// <summary>Lo llama el trigger cuando el jugador lo toca.</summary>
        public void Intentar()
        {
            if (Visible) return;

            if (HayGuardiaEnAlerta())
            {
                _avisoHasta = Time.unscaledTime + DuracionAviso;
                Aviso.SetActive(true);
            }
            else
            {
                Mostrar();
            }
        }

        /// <summary>¿Hay algún guardia de la escena en el tramo Alerta?</summary>
        public static bool HayGuardiaEnAlerta()
        {
            foreach (var guardia in FindObjectsByType<GuardiaAgente>(FindObjectsSortMode.None))
            {
                if (guardia.Pizarra == null) continue;

                NivelSospecha nivel = guardia.Pizarra.Get(ClavesBB.Nivel, NivelSospecha.Tranquilo);
                if (nivel == NivelSospecha.Alerta) return true;
            }
            return false;
        }

        private void Mostrar()
        {
            Visible = true;
            Aviso.SetActive(false);
            Victoria.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>Botón «Reiniciar».</summary>
        public void Reiniciar()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Botón «Salir».</summary>
        public void Salir()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
