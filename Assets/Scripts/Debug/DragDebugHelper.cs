using UnityEngine;
using InsideMatter.Interaction;
using InsideMatter.Molecule;

namespace InsideMatter.Debugging
{
    /// <summary>
    /// Debug-Tool zum Troubleshooting von Drag & Drop Problemen.
    /// Zeigt Raycast-Informationen und gibt Tipps zur Problemlösung.
    /// </summary>
    [RequireComponent(typeof(AtomDrag))]
    public class DragDebugHelper : MonoBehaviour
    {
        [Header("Debug-Visualisierung")]
        [Tooltip("Zeige Raycast-Linie im Scene-View")]
        public bool showRaycast = true;
        
        [Tooltip("Zeige Debug-Infos in der Konsole")]
        public bool logToConsole = true;
        
        [Tooltip("Zeige On-Screen Debug-Text")]
        public bool showOnScreenDebug = true;
        
        private Camera mainCamera;
        private AtomDrag atomDrag;
        private string debugText = "";
        
        void Start()
        {
            mainCamera = Camera.main;
            atomDrag = GetComponent<AtomDrag>();
            
            if (mainCamera == null)
            {
                Debug.LogError("❌ KEINE MAIN CAMERA gefunden! Bitte Kamera mit Tag 'MainCamera' versehen.");
                debugText = "ERROR: Keine Main Camera!";
            }
            else
            {
                Debug.Log("✅ Main Camera gefunden: " + mainCamera.name);
            }
            
            // Prüfe ob Atome in der Szene sind
            Atom[] atoms = FindObjectsByType<Atom>(FindObjectsSortMode.None);
            Debug.Log($"✅ {atoms.Length} Atome in der Szene gefunden");
            
            if (atoms.Length == 0)
            {
                Debug.LogWarning("⚠️ KEINE ATOME in der Szene! Erstelle Atome über GameObject → InsideMatter → Atoms");
            }
            else
            {
                // Prüfe Collider-Setup
                foreach (var atom in atoms)
                {
                    Collider[] colliders = atom.GetComponents<Collider>();
                    if (colliders.Length == 0)
                    {
                        Debug.LogError($"❌ Atom '{atom.name}' hat KEINEN COLLIDER! → Kann nicht geklickt werden");
                    }
                    else
                    {
                        foreach (var col in colliders)
                        {
                            if (col.isTrigger)
                            {
                                Debug.LogWarning($"⚠️ Atom '{atom.name}' hat Trigger-Collider! → Sollte false sein für Raycast");
                            }
                            else
                            {
                                Debug.Log($"✅ Atom '{atom.name}' hat korrekten Collider ({col.GetType().Name})");
                            }
                        }
                    }
                    
                    // Prüfe Layer
                    if (atom.gameObject.layer != 0)
                    {
                        Debug.LogWarning($"⚠️ Atom '{atom.name}' ist auf Layer {LayerMask.LayerToName(atom.gameObject.layer)}. " +
                            $"Prüfe ob AtomDrag LayerMask diesen Layer enthält!");
                    }
                }
            }
        }
        
        void Update()
        {
            if (mainCamera == null) return;
            
            // Raycast durchführen
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            bool didHit = Physics.Raycast(ray, out hit, 100f);
            
            // Debug-Text aktualisieren
            debugText = $"=== DRAG DEBUG ===\n";
            debugText += $"Maus Position: {Input.mousePosition}\n";
            debugText += $"Raycast Hit: {(didHit ? "JA" : "NEIN")}\n";
            
            if (didHit)
            {
                debugText += $"Hit Object: {hit.collider.gameObject.name}\n";
                debugText += $"Hit Position: {hit.point}\n";
                debugText += $"Distance: {hit.distance:F2}m\n";
                
                Atom atom = hit.collider.GetComponent<Atom>();
                if (atom != null)
                {
                    debugText += $"✅ IST EIN ATOM: {atom.element}\n";
                    debugText += $"   Bonds: {atom.CurrentBondCount}/{atom.maxBonds}\n";
                }
                else
                {
                    debugText += $"❌ KEIN ATOM-SCRIPT gefunden!\n";
                }
            }
            else
            {
                debugText += "Zeige auf ein Atom...\n";
            }
            
            // Bei Klick loggen
            if (Input.GetMouseButtonDown(0) && logToConsole)
            {
                if (didHit)
                {
                    Debug.Log($"🖱️ KLICK auf: {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                    
                    Atom atom = hit.collider.GetComponent<Atom>();
                    if (atom != null)
                    {
                        Debug.Log($"   ✅ Atom '{atom.element}' erkannt");
                    }
                    else
                    {
                        Debug.LogWarning($"   ❌ Kein Atom-Component auf diesem Objekt!");
                    }
                }
                else
                {
                    Debug.Log("🖱️ KLICK ins Leere (kein Hit)");
                }
            }
            
            // Raycast visualisieren
            if (showRaycast)
            {
                if (didHit)
                {
                    Debug.DrawLine(ray.origin, hit.point, Color.green);
                    Debug.DrawLine(hit.point, hit.point + hit.normal * 0.5f, Color.blue);
                }
                else
                {
                    Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red);
                }
            }
        }
        
        void OnGUI()
        {
            if (!showOnScreenDebug) return;
            
            // Debug-Box
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            GUI.Box(new Rect(10, 10, 300, 180), debugText, style);
            
            // Hilfe-Text
            string helpText = "HILFE:\n" +
                "• Maus über Atom bewegen\n" +
                "• Linke Maustaste halten\n" +
                "• Ziehen & loslassen\n" +
                "\n" +
                "Probleme?\n" +
                "→ Siehe Konsole für Details!";
            
            GUI.Box(new Rect(10, 200, 300, 150), helpText, style);
        }
    }
}
