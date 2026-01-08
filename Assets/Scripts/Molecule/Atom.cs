using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Repräsentiert ein einzelnes Atom mit seinen chemischen Eigenschaften
    /// und Bindungspunkten (BondPoints) für molekulare Verbindungen.
    /// </summary>
    public class Atom : MonoBehaviour
    {
        [Header("Chemische Eigenschaften")]
        [Tooltip("Element-Symbol (z.B. C, H, O, N)")]
        public string element = "C";
        
        [Tooltip("Maximale Anzahl von Bindungen (Valenz)")]
        public int maxBonds = 4;
        
        [Header("Visuelle Eigenschaften")]
        [Tooltip("Farbe des Atoms")]
        public Color atomColor = Color.gray;
        
        [Tooltip("Radius der Atom-Kugel")]
        public float atomRadius = 0.3f;
        
        [Header("Bindungspunkte")]
        [Tooltip("Liste aller BondPoints dieses Atoms")]
        public List<BondPoint> bondPoints = new List<BondPoint>();

        // Chemisch korrekte Geometrien (Vektoren von Zentrum zu BondPoint)
        private static readonly Dictionary<string, Vector3[]> ElementGeometries = new Dictionary<string, Vector3[]>()
        {
        { "H", new Vector3[] { Vector3.forward } }, // 1 Bindung
            { "Fl", new Vector3[] { Vector3.forward } },
            { "Cl", new Vector3[] { Vector3.forward } },
            { "Na", new Vector3[] { Vector3.forward } },
            
            { "O", new Vector3[] { // 2 Bindungen (Winkel ca. 104.5°)
                new Vector3(0, 0.407f, 0.914f),
                new Vector3(0, -0.407f, 0.914f)
            } },
            { "S", new Vector3[] { // 2 Bindungen (Winkel ca. 104.5°)
                new Vector3(0, 0.407f, 0.914f),
                new Vector3(0, -0.407f, 0.914f)
            } },
            
            { "N", new Vector3[] { // 3 Bindungen (Tripodal / Pyramidal)
                new Vector3(0.94f, 0, -0.33f),
                new Vector3(-0.47f, 0.81f, -0.33f),
                new Vector3(-0.47f, -0.81f, -0.33f)
            } },
            
            { "C", new Vector3[] { // 4 Bindungen (Tetraedrisch)
                new Vector3(1, 1, 1).normalized,
                new Vector3(1, -1, -1).normalized,
                new Vector3(-1, 1, -1).normalized,
                new Vector3(-1, -1, 1).normalized
            } },

            { "Fe", new Vector3[] { // 3 Bindungen (Pyramidal wie N)
                new Vector3(0.94f, 0, -0.33f),
                new Vector3(-0.47f, 0.81f, -0.33f),
                new Vector3(-0.47f, -0.81f, -0.33f)
            } },
            { "Ca", new Vector3[] { // 2 Bindungen (Gewinkelt wie O)
                new Vector3(0, 0.407f, 0.914f),
                new Vector3(0, -0.407f, 0.914f)
            } }
        };
       
        // Logische Verbindungen zu anderen Atomen
        private List<Atom> connectedAtoms = new List<Atom>();
        
        // Referenz zum Renderer für Farbänderungen
        private Renderer atomRenderer;
        
        // Ist dieses Atom gerade ausgewählt/gedragged?
        private bool isSelected = false;
        
        // Valenz-Anzeige (3D Text über dem Atom)
        private TextMeshPro valenceLabel;
        private int lastDisplayedValence = -1;
        
        /// <summary>
        /// Wurde dieses Atom jemals vom Spieler aufgehoben?
        /// Atome die noch nie gegriffen wurden, können keine Bindungen eingehen.
        /// </summary>
        public bool WasEverGrabbed { get; set; } = false;
        
        /// <summary>
        /// Aktuelle Anzahl der Bindungen
        /// </summary>
        public int CurrentBondCount => bondPoints.Count(bp => bp.Occupied);
        
        /// <summary>
        /// Hat dieses Atom noch freie Bindungen?
        /// Gibt nur true zurück wenn das Atom schon einmal gegriffen wurde.
        /// Berücksichtigt Bindungsordnung: Doppelbindung = 2, Dreifachbindung = 3
        /// </summary>
        public bool HasFreeBond
        {
            get
            {
                if (!WasEverGrabbed) return false;
                int usedValence = MoleculeManager.Instance?.CalculateCurrentValence(this) ?? 0;
                return usedValence < maxBonds;
            }
        }
        
        /// <summary>
        /// Liste der verbundenen Atome
        /// </summary>
        public IReadOnlyList<Atom> ConnectedAtoms => connectedAtoms.AsReadOnly();
        
        void Awake()
        {
            atomRenderer = GetComponent<Renderer>();
            if (atomRenderer == null) atomRenderer = GetComponentInChildren<Renderer>();
            
            // Geometrie festlegen (Factual)
            UpdateBondPoints();
        }
       
        void Start()
        {
            ApplyVisuals();
            CreateValenceLabel();
        }
        
        void Update()
        {
            // Billboard-Effekt: Valenz-Label zeigt immer zur Kamera
            if (valenceLabel != null && Camera.main != null)
            {
                valenceLabel.transform.LookAt(Camera.main.transform);
                valenceLabel.transform.Rotate(0, 180, 0);
            }
            
            // Valenz aktualisieren wenn sich etwas geändert hat
            UpdateValenceLabel();
        }
        
        /// <summary>
        /// Erstellt das 3D-Label für die Valenz-Anzeige
        /// </summary>
        private void CreateValenceLabel()
        {
            GameObject labelObj = new GameObject("ValenceLabel");
            labelObj.transform.SetParent(transform);
            labelObj.transform.localPosition = new Vector3(0, 1.5f, 0); // Über dem Atom
            
            valenceLabel = labelObj.AddComponent<TextMeshPro>();
            valenceLabel.fontSize = 3f;
            valenceLabel.alignment = TextAlignmentOptions.Center;
            valenceLabel.color = Color.white;
            valenceLabel.outlineWidth = 0.2f;
            valenceLabel.outlineColor = Color.black;
            
            // RectTransform für korrekte Größe
            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(2f, 1f);
            
            UpdateValenceLabel();
        }
        
        /// <summary>
        /// Aktualisiert die Valenz-Anzeige
        /// </summary>
        public void UpdateValenceLabel()
        {
            if (valenceLabel == null) return;
            
            // Berechne verfügbare Valenzen (berücksichtigt Bindungsordnung)
            int usedValence = 0;
            if (MoleculeManager.Instance != null)
            {
                usedValence = MoleculeManager.Instance.CalculateCurrentValence(this);
            }
            int freeValence = maxBonds - usedValence;
            
            // Nur aktualisieren wenn sich etwas geändert hat
            if (freeValence == lastDisplayedValence) return;
            lastDisplayedValence = freeValence;
            
            if (freeValence > 0)
            {
                valenceLabel.text = $"×{freeValence}";
                valenceLabel.gameObject.SetActive(true);
            }
            else
            {
                valenceLabel.gameObject.SetActive(false); // Keine freien Bindungen = verstecken
            }
        }
        
        /// <summary>
        /// Wendet die visuellen Eigenschaften (Farbe, Größe) auf das Atom an
        /// </summary>
        public void ApplyVisuals()
        {
            if (atomRenderer != null)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                props.SetColor("_BaseColor", atomColor);
                props.SetColor("_Color", atomColor); // Fallback für andere Shader
                atomRenderer.SetPropertyBlock(props);
            }
            
            transform.localScale = Vector3.one * atomRadius;
            
            // BondPoint Visuals nach einem Frame erstellen (wenn lossyScale korrekt ist)
            StartCoroutine(CreateBondPointVisualsDelayed());
        }
        
        private System.Collections.IEnumerator CreateBondPointVisualsDelayed()
        {
            // Warte 2 Frames damit alle Skalierungen angewendet sind
            yield return null;
            yield return null;
            
            // ALLE alten Visuals löschen
            foreach (var bp in bondPoints)
            {
                if (bp == null) continue;
                for (int i = bp.transform.childCount - 1; i >= 0; i--)
                {
                    Destroy(bp.transform.GetChild(i).gameObject);
                }
            }
            
            yield return null; // Warte bis gelöscht
            
            // FESTE ABSOLUTE WELT-GRÖSSE: 8mm = 0.008 Einheiten
            // Trick: Erstelle das Visual OHNE Parent, setze absolute Größe, DANN parent
            float absoluteWorldSize = 0.008f;
            
            foreach (var bp in bondPoints)
            {
                if (bp == null) continue;
                
                // Visual erstellen (NOCH NICHT PARENTED)
                GameObject vgo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vgo.name = "Visual";
                
                // Absolute Welt-Skalierung setzen BEVOR wir parenten
                vgo.transform.localScale = Vector3.one * absoluteWorldSize;
                
                // Jetzt parenten (Unity passt localScale automatisch an)
                vgo.transform.SetParent(bp.transform, worldPositionStays: true);
                vgo.transform.localPosition = Vector3.zero;
                
                // Collider entfernen
                Destroy(vgo.GetComponent<Collider>());
                
                // Material: Weiß
                Renderer rend = vgo.GetComponent<Renderer>();
                if (rend != null)
                {
                    Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                    mat.color = Color.white;
                    mat.SetColor("_BaseColor", Color.white);
                    rend.sharedMaterial = mat;
                }
            }
            
            Debug.Log($"[Atom {element}] BondPoint Visuals erstellt mit Welt-Größe {absoluteWorldSize}m");
        }
        
        /// <summary>
        /// Setzt das Atom als ausgewählt/nicht ausgewählt
        /// </summary>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            
            if (atomRenderer != null)
            {
                MaterialPropertyBlock props = new MaterialPropertyBlock();
                Color highlightColor = selected ? Color.Lerp(atomColor, Color.white, 0.5f) : atomColor;
                props.SetColor("_BaseColor", highlightColor);
                props.SetColor("_Color", highlightColor);
                atomRenderer.SetPropertyBlock(props);
            }
        }
        
        /// <summary>
        /// Registriert eine Verbindung zu einem anderen Atom
        /// </summary>
        public void AddConnection(Atom other)
        {
            if (!connectedAtoms.Contains(other))
            {
                connectedAtoms.Add(other);
            }
        }
        
        /// <summary>
        /// Entfernt eine Verbindung zu einem anderen Atom
        /// </summary>
        public void RemoveConnection(Atom other)
        {
            connectedAtoms.Remove(other);
        }
        
        /// <summary>
        /// Findet den nächsten freien BondPoint
        /// </summary>
        public BondPoint GetFreeBondPoint()
        {
            return bondPoints.FirstOrDefault(bp => !bp.Occupied);
        }
        
        /// <summary>
        /// Richtet die BondPoints chemisch korrekt aus.
        /// Wird vom SceneDecorator oder via ContextMenu aufgerufen.
        /// </summary>
        [ContextMenu("Fix Geometry (Factual)")]
        public void UpdateBondPoints()
        {
            Vector3[] directions = null;
            
            if (ElementGeometries.ContainsKey(element))
            {
                directions = ElementGeometries[element];
                maxBonds = directions.Length;
            }
            else
            {
                Debug.LogWarning($"[Atom] Keine Geometrie-Daten für Element {element} - verwende existierende BondPoints.");
                // Fahre trotzdem fort um existierende BondPoints zu aktualisieren
            }

            // Vorhandene Punkte sammeln
            var currentPoints = GetComponentsInChildren<BondPoint>(true).ToList();
            
            // Nur wenn wir Geometrie-Daten haben, Punkte anpassen
            if (directions != null)
            {
                // Zu viele Punkte?
                while (currentPoints.Count > maxBonds)
                {
                    var p = currentPoints[currentPoints.Count - 1];
                    currentPoints.RemoveAt(currentPoints.Count - 1);
                    if (Application.isPlaying) Destroy(p.gameObject);
                    else DestroyImmediate(p.gameObject);
                }

                // Zu wenige Punkte?
                while (currentPoints.Count < maxBonds)
                {
                    GameObject go = new GameObject($"BondPoint_{currentPoints.Count}");
                    go.transform.SetParent(this.transform);
                    var bp = go.AddComponent<BondPoint>();
                    currentPoints.Add(bp);
                }
            }

            bondPoints = currentPoints;

            // Punkte positionieren und Visuals aktualisieren
            float dist = 0.5f; 
            for (int i = 0; i < bondPoints.Count; i++)
            {
                // Positionierung nur wenn wir Geometrie-Daten haben
                if (directions != null && i < directions.Length)
                {
                    bondPoints[i].transform.localPosition = directions[i] * dist;
                    bondPoints[i].transform.localRotation = Quaternion.LookRotation(directions[i]);
                }
                
                bondPoints[i].ParentAtom = this;
                bondPoints[i].name = $"BondPoint_{element}_{i}";
                
                // Visuals werden separat in CreateBondPointVisualsDelayed() erstellt
            }
            Debug.Log($"[Atom] Geometrie für {element} aktualisiert ({maxBonds} Punkte).");
        }

        /// <summary>
        /// Debug-Visualisierung der BondPoints
        /// </summary>
        void OnDrawGizmos()
        {
            if (bondPoints == null) return;
            
            Gizmos.color = Color.yellow;
            foreach (var bp in bondPoints)
            {
                if (bp != null)
                {
                    Gizmos.DrawWireSphere(bp.transform.position, 0.1f);
                    Gizmos.DrawLine(transform.position, bp.transform.position);
                }
            }
        }
    }
}
