// ═══════════════════════════════════════════════════════════════════════════
//  CrearPanelVictoria.cs — Monta la UI de victoria en la escena con uGUI
//
//  Menú: Tools > Victoria > Crear panel de victoria   (con SampleScene abierta)
//
//  Crea, dentro del Canvas (y si no hay, crea uno con su EventSystem):
//    PanelVictoria          ← PanelVictoria (siempre activo)
//      Victoria             ← fondo oscuro + caja (desactivado al inicio)
//        Caja
//          Titulo           «¡VICTORIA!»
//          BotonReiniciar   Button
//          BotonSalir       Button
//      Aviso                «¡Te están buscando! ¡Escóndete!» (desactivado)
//
//  Se puede ejecutar una sola vez. Si ya existe un PanelVictoria, no hace nada.
// ═══════════════════════════════════════════════════════════════════════════

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using UDIT.IA3.Victoria;

namespace UDIT.IA3.Victoria.Herramientas
{
    public static class CrearPanelVictoria
    {
        [MenuItem("Tools/Victoria/Crear panel de victoria")]
        static void Crear()
        {
            if (Object.FindAnyObjectByType<PanelVictoria>() != null)
            {
                Debug.LogWarning("Ya hay un PanelVictoria en la escena.");
                return;
            }

            // ── Canvas y EventSystem ───────────────────────────────────────
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var goCanvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(goCanvas, "Crear panel de victoria");
                canvas = goCanvas.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = goCanvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var goEvt = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(goEvt, "Crear panel de victoria");
            }

            // ── Jerarquía ──────────────────────────────────────────────────
            GameObject raiz = Nuevo("PanelVictoria", canvas.transform);
            Undo.RegisterCreatedObjectUndo(raiz, "Crear panel de victoria");
            Estirar(raiz);
            PanelVictoria panel = raiz.AddComponent<PanelVictoria>();

            GameObject victoria = Nuevo("Victoria", raiz.transform);
            Estirar(victoria);
            victoria.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            GameObject caja = Nuevo("Caja", victoria.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 340));
            caja.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

            Texto(caja.transform, "Titulo", "¡VICTORIA!", 72, new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(520, 90), new Color(1f, 0.85f, 0.2f));
            GameObject btnReiniciar = Boton(caja.transform, "BotonReiniciar", "Reiniciar", new Vector2(0, -10));
            GameObject btnSalir = Boton(caja.transform, "BotonSalir", "Salir", new Vector2(0, -100));

            GameObject aviso = Texto(raiz.transform, "Aviso", "¡Te están buscando! ¡Escóndete!", 44,
                new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1200, 80), new Color(1f, 0.25f, 0.25f));
            aviso.SetActive(false);

            // ── Referencias del script ─────────────────────────────────────
            panel.Victoria = victoria;
            panel.Aviso = aviso;
            panel.BotonReiniciar = btnReiniciar.GetComponent<Button>();
            panel.BotonSalir = btnSalir.GetComponent<Button>();
            victoria.SetActive(false);

            EditorSceneManager.MarkSceneDirty(raiz.scene);
            Selection.activeGameObject = raiz;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        static GameObject Nuevo(string nombre, Transform padre, Vector2 ancla = default, Vector2 pos = default, Vector2 tamano = default)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            var rt = (RectTransform)go.transform;
            if (ancla == default) ancla = new Vector2(0.5f, 0.5f);
            rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
            rt.anchoredPosition = pos;
            rt.sizeDelta = tamano == default ? new Vector2(100, 100) : tamano;
            return go;
        }

        static void Estirar(GameObject go)
        {
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        static GameObject Texto(Transform padre, string nombre, string texto, float tam,
                                Vector2 ancla, Vector2 pos, Vector2 tamano, Color color)
        {
            GameObject go = Nuevo(nombre, padre, ancla, pos, tamano);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = texto;
            t.fontSize = tam;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.raycastTarget = false;
            return go;
        }

        static GameObject Boton(Transform padre, string nombre, string texto, Vector2 pos)
        {
            GameObject go = Nuevo(nombre, padre, new Vector2(0.5f, 0.5f), pos, new Vector2(320, 60));
            go.AddComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
            go.AddComponent<Button>();

            GameObject etiqueta = Nuevo("Texto", go.transform);
            Estirar(etiqueta);
            var t = etiqueta.AddComponent<TextMeshProUGUI>();
            t.text = texto;
            t.fontSize = 32;
            t.alignment = TextAlignmentOptions.Center;
            t.color = Color.black;
            t.raycastTarget = false;
            return go;
        }
    }
}
