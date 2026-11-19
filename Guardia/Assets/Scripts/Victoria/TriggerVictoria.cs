// ═══════════════════════════════════════════════════════════════════════════
//  TriggerVictoria.cs — Zona de salida. Al tocarla, el jugador intenta ganar.
//
//  Colocad un objeto con un Collider marcado como «Is Trigger» (al añadir el
//  componente se marca solo). El jugador tiene que llevar el tag «Player».
//  La decisión de mostrar la victoria o el aviso la toma PanelVictoria.
// ═══════════════════════════════════════════════════════════════════════════

using UnityEngine;

namespace UDIT.IA3.Victoria
{
    [RequireComponent(typeof(Collider))]
    public class TriggerVictoria : MonoBehaviour
    {
        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider otro)
        {
            if (!otro.CompareTag("Player")) return;

            if (PanelVictoria.Instancia == null)
            {
                Debug.LogWarning("TriggerVictoria: no hay ningún PanelVictoria en la escena.", this);
                return;
            }

            PanelVictoria.Instancia.Intentar();
        }
    }
}
