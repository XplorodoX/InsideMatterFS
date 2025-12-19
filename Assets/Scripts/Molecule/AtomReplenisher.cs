using UnityEngine;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables; // Falls XRI 3.x
using InsideMatter.Interaction; // Für VRAtomGrab

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Spawns an atom and ensures there is always one available.
    /// If the current atom is taken away, a new one is spawned.
    /// </summary>
    public class AtomReplenisher : MonoBehaviour
    {
        [Header("Settings")]
        public GameObject atomPrefab;
        public float checkDistance = 0.15f; // Ab wann gilt das Atom als "weg" (etwas toleranter)
        public float spawnCheckRadius = 0.05f; // Kleinerer Radius, damit es nicht so leicht blockiert wird
        public float respawnDelay = 1.0f; // Wartezeit bevor neu gespawnt wird

        private GameObject currentAtom;
        private bool isRespawning = false;

        void Start()
        {
            // Direkt am Anfang spawnen (ohne Delay)
            SpawnNow();
        }

        void Update()
        {
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

            Vector3 spawnPos = transform.position + Vector3.up * 0.1f;
            currentAtom = Instantiate(atomPrefab, spawnPos, Quaternion.identity);
            
            // Skalierung (Verkleinert auf 0.4 für bessere VR-Haptik)
            currentAtom.transform.localScale *= 0.4f; 
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
