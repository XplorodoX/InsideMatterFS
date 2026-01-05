using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Falls XRI 3.x
using InsideMatter.Interaction; // Für VRAtomGrab
using TMPro;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Spawns an atom and ensures there is always one available.
    /// If the current atom is taken away, a new one is spawned.
    /// Displays a label showing what element this tray contains.
    /// </summary>
    public class AtomReplenisher : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject atomPrefab;
        public float checkDistance = 0.15f; // Ab wann gilt das Atom als "weg" (etwas toleranter)
        public float spawnCheckRadius = 0.05f; // Kleinerer Radius, damit es nicht so leicht blockiert wird
        public float respawnDelay = 1.0f; // Wartezeit bevor neu gespawnt wird

        [Header("Label Einstellungen")]
        [Tooltip("Zeigt ein 3D-Label mit dem Elementnamen über dem Tray")]
        public bool showLabel = true;
        
        [Tooltip("Höhe des Labels über dem Tray")]
        public float labelHeight = 0.25f;
        
        [Tooltip("Schriftgröße des Labels")]
        public float labelFontSize = 0.8f;
        
        [Tooltip("Farbe des Label-Textes")]
        public Color labelColor = Color.white;

        private GameObject currentAtom;
        private bool isRespawning = false;
        private TextMeshPro labelText;

        void Start()
        {
            // Label erstellen (vor dem Spawn, damit es sofort sichtbar ist)
            if (showLabel)
            {
                CreateLabel();
            }
            
            // Direkt am Anfang spawnen (ohne Delay)
            SpawnNow();
        }
        
        /// <summary>
        /// Erstellt ein 3D-Text-Label über der Petrischale mit dem Elementnamen
        /// </summary>
        private void CreateLabel()
        {
            // Element-Info aus dem Prefab auslesen
            string elementSymbol = "?";
            string elementName = "Unbekannt";
            Color elementColor = Color.white;
            
            if (atomPrefab != null)
            {
                Atom prefabAtom = atomPrefab.GetComponent<Atom>();
                if (prefabAtom != null)
                {
                    elementSymbol = prefabAtom.element;
                    elementColor = prefabAtom.atomColor;
                }
            }
            
            // Label GameObject erstellen
            GameObject labelObj = new GameObject($"Label_{elementSymbol}");
            labelObj.transform.SetParent(transform);
            labelObj.transform.localPosition = new Vector3(0, labelHeight, 0);
            labelObj.transform.localRotation = Quaternion.identity;
            
            // TextMeshPro hinzufügen
            labelText = labelObj.AddComponent<TextMeshPro>();
            labelText.text = $"<size=200%><b>{elementSymbol}</b></size>";
            labelText.fontSize = labelFontSize * 1.5f; // Größer, da nur noch ein Buchstabe
            labelText.color = labelColor;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.enableAutoSizing = false;
            
            // RectTransform für korrekte Größe
            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 1f);
            
            // Optional: Leichten farbigen Hintergrund passend zum Element
            // Das Label dreht sich zum Spieler (Billboard-Effekt wird in Update gemacht)
        }

        void Update()
        {
            // Billboard-Effekt: Label zeigt immer zur Kamera
            if (labelText != null)
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    labelText.transform.LookAt(mainCam.transform);
                    labelText.transform.Rotate(0, 180, 0); // Text nicht gespiegelt
                }
            }
            
            // Wenn wir ein Atom haben, prüfen WANN es weg ist
            if (currentAtom != null)
            {
                float distance = Vector3.Distance(currentAtom.transform.position, transform.position);
                if (distance > checkDistance)
                {
                    currentAtom = null; // Platz ist logisch gesehen frei
                }
            }
            
            // Wenn KEIN Atom da ist und wir noch nicht dabei sind, eins zu machen -> Start Process
            if (currentAtom == null && !isRespawning)
            {
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            isRespawning = true;

            // 1. Warte die gewünschte Verzögerung (damit man Zeit hat, die Hand wegzunehmen)
            yield return new WaitForSeconds(respawnDelay);

            // 2. Warte SOLANGE, bis der Platz wirklich frei ist
            while (!IsSpaceClear())
            {
                yield return new WaitForSeconds(0.1f); // Check alle 0.1s
            }

            // 3. Spawnen
            SpawnNow();
            isRespawning = false;
        }

        private bool IsSpaceClear()
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            Collider[] hits = Physics.OverlapSphere(spawnPos, spawnCheckRadius);
            
            foreach (var hit in hits)
            {
                // Ignoriere den Tray selbst und den Tisch
                if (hit.gameObject == this.gameObject || hit.gameObject.name.Contains("Table") || hit.gameObject.name.Contains("Bench")) 
                    continue;

                // Etwas ist im Weg!
                return false;
            }
            return true;
        }

        private void SpawnNow()
        {
             if (atomPrefab == null) return;

            // Skalierung (Verkleinert auf 0.4 für bessere VR-Haptik)
            float spawnScale = 0.4f;
            
            // Spawn-Höhe: 18cm über dem Tray-Zentrum für gute Sichtbarkeit
            float heightAboveTray = 0.18f;
            Vector3 spawnPos = transform.position + Vector3.up * heightAboveTray;
            
            currentAtom = Instantiate(atomPrefab, spawnPos, Quaternion.identity);
            
            currentAtom.transform.localScale *= spawnScale; 
            // Kollision Tray ignorieren
            Collider atomCol = currentAtom.GetComponent<Collider>();
            Collider trayCol = GetComponent<Collider>();
            if (atomCol != null && trayCol != null) Physics.IgnoreCollision(atomCol, trayCol, true);

            // XRI
            var grab = currentAtom.GetComponent<XRGrabInteractable>();
            if (grab == null) grab = currentAtom.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            
            // VR Chemical Logic (GHOST PREVIEW & BONDING)
            if (currentAtom.GetComponent<VRAtomGrab>() == null)
            {
                currentAtom.AddComponent<VRAtomGrab>();
            }

            // Physics (Schweben)
            var rb = currentAtom.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false; 
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
