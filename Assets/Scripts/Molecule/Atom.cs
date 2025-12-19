using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            { "O", new Vector3[] { // 2 Bindungen (Winkel ca. 104.5°)
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
            } }
        };
       
        // Logische Verbindungen zu anderen Atomen
        private List<Atom> connectedAtoms = new List<Atom>();
        
        // Referenz zum Renderer für Farbänderungen
        private Renderer atomRenderer;
        
        // Ist dieses Atom gerade ausgewählt/gedragged?
        private bool isSelected = false;
        
        /// <summary>
        /// Aktuelle Anzahl der Bindungen
        /// </summary>
        public int CurrentBondCount => bondPoints.Count(bp => bp.Occupied);
        
        /// <summary>
        /// Hat dieses Atom noch freie Bindungen?
        /// </summary>
        public bool HasFreeBond => CurrentBondCount < maxBonds;
        
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
            if (!ElementGeometries.ContainsKey(element))
            {
                Debug.LogWarning($"[Atom] Keine Geometrie-Daten für Element {element} hinterlegt.");
                return;
            }

            Vector3[] directions = ElementGeometries[element];
            maxBonds = directions.Length;

            // Vorhandene Punkte bereinigen/sammeln
            var currentPoints = GetComponentsInChildren<BondPoint>(true).ToList();
            
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

            bondPoints = currentPoints;

            // Punkte positionieren
            // Ein Standard-Primitive Sphere (Radius 0.5) skaliert mit transform.localScale hat echten world-Radius (0.5 * atomRadius)
            // Um an der Oberfläche zu sein, muss der lokale Abstand also genau 0.5f sein.
            float dist = 0.5f; 
            for (int i = 0; i < bondPoints.Count; i++)
            {
                bondPoints[i].transform.localPosition = directions[i] * dist;
                bondPoints[i].transform.localRotation = Quaternion.LookRotation(directions[i]);
                bondPoints[i].ParentAtom = this;
                bondPoints[i].name = $"BondPoint_{element}_{i}";

                // VISUELLE ANKERPUNKTE (Gelbe Kugeln)
                Transform visual = bondPoints[i].transform.Find("Visual");
                if (visual == null)
                {
                    GameObject vgo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    vgo.name = "Visual";
                    vgo.transform.SetParent(bondPoints[i].transform);
                    vgo.transform.localPosition = Vector3.zero;
                    // Skalierung an atomRadius anpassen, damit die Punkte immer gleich groß erscheinen
                    vgo.transform.localScale = Vector3.one * (0.25f / atomRadius); 
                    DestroyImmediate(vgo.GetComponent<Collider>());
                    visual = vgo.transform;

                    // Material (Gelb & Leuchtend)
                    Renderer rend = vgo.GetComponent<Renderer>();
                    
                    // Sicherer Weg um Shader in Builds zu finden: Nutze den Shader vom MoleculeManager Material
                    Shader urpShader = null;
                    if (MoleculeManager.Instance != null && MoleculeManager.Instance.bondMaterial != null)
                    {
                        urpShader = MoleculeManager.Instance.bondMaterial.shader;
                    }
                    else
                    {
                        urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    }

                    Material mat = new Material(urpShader);
                    mat.color = Color.yellow;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.yellow * 0.5f);
                    rend.sharedMaterial = mat;
                }
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
