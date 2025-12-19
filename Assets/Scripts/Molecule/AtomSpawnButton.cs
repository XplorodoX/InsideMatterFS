using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit; // Basis Namespace
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Für XRBaseInteractable in XRI 3.x

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Ermöglicht es, den AtomSpawner durch Berührung oder Klick in VR zu triggern.
    /// </summary>
    public class AtomSpawnButton : MonoBehaviour
    {
        public AtomSpawner spawner;
        public int atomIndex = 0;
        
        [Header("Visuals")]
        public Color hoverColor = Color.yellow;
        private Color originalColor;
        private Renderer rend;

        void Start()
        {
            Debug.Log($"[AtomSpawnButton] Initializing for atom index {atomIndex}...");
            rend = GetComponent<Renderer>();
            if (rend != null) originalColor = rend.material.color;
            if (spawner == null) 
            {
                spawner = FindFirstObjectByType<AtomSpawner>();
                if(spawner == null) Debug.LogError("[AtomSpawnButton] KEIN SPAWNER GEFUNDEN!");
            }

            // XR Interactable Support
            var interactable = GetComponent<XRBaseInteractable>();
            if (interactable != null)
            {
                Debug.Log("[AtomSpawnButton] XR Interactable gefunden. Verknüpfe Events...");
                interactable.selectEntered.AddListener((args) => 
                {
                    Debug.Log($"[AtomSpawnButton] SelectEntered triggered on {gameObject.name}");
                    OnPress();
                });
                interactable.activated.AddListener((args) => 
                {
                    Debug.Log($"[AtomSpawnButton] Activated triggered on {gameObject.name}");
                    OnPress();
                });
                // Versuch auch Hover, falls Poke als Hover zählt
                interactable.hoverEntered.AddListener((args) =>
                {
                    // Optional: Feedback bei Hover
                });
            }
            else
            {
                Debug.LogWarning("[AtomSpawnButton] KEIN XRBaseInteractable gefunden!");
            }
        }

        public void OnPress()
        {
            Debug.Log($"[AtomSpawnButton] OnPress! Spawning Atom {atomIndex}...");
            if (spawner != null)
            {
                spawner.SpawnAtom(atomIndex);
                
                // Visueller Effekt
                if (GetComponent("XRPokeFollowAffordance") == null)
                {
                    transform.localScale *= 0.9f;
                    Invoke("ResetScale", 0.1f);
                }
            }
            else
            {
                Debug.LogError("[AtomSpawnButton] Spawner ist NULL. Kann nicht spawnen.");
            }
        }

        private void ResetScale() => transform.localScale = Vector3.one;

        // XRI Trigger (Fallschirm für Kollision)
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") || other.name.Contains("Hand") || other.name.Contains("Controller"))
            {
                // Nur wenn kein Interactable da ist (doppeltes Spawnen vermeiden)
                if (GetComponent<XRBaseInteractable>() == null)
                {
                    OnPress();
                }
            }
        }

        // Diese Funktion wird im Editor aufgerufen, wenn sich Werte ändern oder das Skript lädt.
        // Dadurch füllt sich das Feld "Spawner" automatisch aus!
        private void OnValidate()
        {
            if (spawner == null)
            {
                // Versuche den Spawner automatisch in der Szene zu finden
                spawner = FindFirstObjectByType<AtomSpawner>();
            }
        }
    }
}
