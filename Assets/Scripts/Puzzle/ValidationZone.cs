using UnityEngine;
using System.Collections.Generic;
using InsideMatter.Molecule;
using TMPro;

namespace InsideMatter.Puzzle
{
    /// <summary>
    /// Validierungszone auf dem Abgabe-Tisch.
    /// Hier platziert der Spieler sein fertiges Molekül zur Überprüfung.
    /// </summary>
    public class ValidationZone : MonoBehaviour
    {
        [Header("Zone Einstellungen")]
        [Tooltip("Radius der Validierungszone")]
        public float zoneRadius = 0.4f;
        
        [Tooltip("Höhe der Zone über dem Tisch")]
        public float zoneHeight = 0.3f;
        
        [Header("Visuelle Effekte")]
        [Tooltip("Material für den leuchtenden Bereich")]
        public Material zoneMaterial;
        
        [Tooltip("Farbe wenn leer (wartend)")]
        public Color emptyColor = new Color(0.2f, 0.5f, 1f, 0.5f); // Blau
        
        [Tooltip("Farbe wenn Molekül erkannt")]
        public Color detectedColor = new Color(1f, 0.8f, 0.2f, 0.7f); // Gelb
        
        [Tooltip("Farbe bei Erfolg")]
        public Color successColor = new Color(0.2f, 1f, 0.3f, 0.8f); // Grün
        
        [Tooltip("Farbe bei Fehler")]
        public Color errorColor = new Color(1f, 0.2f, 0.2f, 0.8f); // Rot
        
        [Tooltip("Pulsieren der Zone")]
        public float pulseSpeed = 2f;
        public float pulseIntensity = 0.3f;
        
        [Header("UI")]
        [Tooltip("Statustext über der Zone")]
        public TextMeshPro statusText;
        
        // Interne Variablen
        private List<Atom> atomsInZone = new List<Atom>();
        private MeshRenderer zoneRenderer;
        private float pulseTimer = 0f;
        private Color currentBaseColor;
        private bool isValidating = false;
        
        /// <summary>
        /// Gibt alle Atome in der Zone zurück
        /// </summary>
        public List<Atom> AtomsInZone => atomsInZone;
        
        /// <summary>
        /// Ist mindestens ein Atom in der Zone?
        /// </summary>
        public bool HasMolecule => atomsInZone.Count > 0;
        
        void Awake()
        {
            CreateZoneVisual();
            currentBaseColor = emptyColor;
            UpdateStatus("Platziere dein Molekül hier");
        }
        
        void Update()
        {
            if (isValidating) return;
            
            // Pulsieren
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = Mathf.Sin(pulseTimer) * pulseIntensity;
            
            Color displayColor = currentBaseColor;
            displayColor.a = Mathf.Clamp01(currentBaseColor.a + pulse);
            
            if (zoneMaterial != null)
            {
                zoneMaterial.SetColor("_BaseColor", displayColor);
            }
            
            // Farbe basierend auf Inhalt
            if (atomsInZone.Count > 0)
            {
                currentBaseColor = detectedColor;
                UpdateStatus($"Molekül erkannt ({atomsInZone.Count} Atome)\nDrücke PRÜFEN!");
            }
            else
            {
                currentBaseColor = emptyColor;
                UpdateStatus("Platziere dein Molekül hier");
            }
        }
        
        /// <summary>
        /// Erstellt die visuelle Darstellung der Zone
        /// </summary>
        private void CreateZoneVisual()
        {
            // Zylinder als Basis
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ZoneVisual";
            visual.transform.SetParent(transform);
            visual.transform.localPosition = new Vector3(0, zoneHeight / 2f, 0);
            visual.transform.localScale = new Vector3(zoneRadius * 2f, 0.02f, zoneRadius * 2f);
            
            // Collider als Trigger
            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null) Destroy(visualCollider);
            
            // Material erstellen
            zoneRenderer = visual.GetComponent<MeshRenderer>();
            zoneMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            zoneMaterial.SetFloat("_Surface", 1); // Transparent
            zoneMaterial.SetFloat("_Blend", 0);
            zoneMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            zoneMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            zoneMaterial.SetInt("_ZWrite", 0);
            zoneMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            zoneMaterial.renderQueue = 3000;
            zoneMaterial.SetColor("_BaseColor", emptyColor);
            zoneRenderer.material = zoneMaterial;
            
            // Trigger-Collider für Erkennung
            SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = zoneRadius;
            trigger.center = new Vector3(0, zoneHeight / 2f, 0);
            
            // Statustext erstellen
            if (statusText == null)
            {
                GameObject textObj = new GameObject("StatusText");
                textObj.transform.SetParent(transform);
                textObj.transform.localPosition = new Vector3(0, zoneHeight + 0.15f, 0);
                
                statusText = textObj.AddComponent<TextMeshPro>();
                statusText.fontSize = 0.5f;
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.color = Color.white;
                
                RectTransform rect = textObj.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(1f, 0.5f);
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            Atom atom = other.GetComponentInParent<Atom>();
            if (atom != null && !atomsInZone.Contains(atom))
            {
                atomsInZone.Add(atom);
                Debug.Log($"[ValidationZone] Atom entered: {atom.element}");
            }
        }
        
        void OnTriggerExit(Collider other)
        {
            Atom atom = other.GetComponentInParent<Atom>();
            if (atom != null && atomsInZone.Contains(atom))
            {
                atomsInZone.Remove(atom);
                Debug.Log($"[ValidationZone] Atom exited: {atom.element}");
            }
        }
        
        /// <summary>
        /// Aktualisiert den Statustext
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
        
        /// <summary>
        /// Zeigt Erfolgs-Feedback
        /// </summary>
        public void ShowSuccess(string message = "✓ Richtig!")
        {
            isValidating = true;
            currentBaseColor = successColor;
            if (zoneMaterial != null)
            {
                zoneMaterial.SetColor("_BaseColor", successColor);
            }
            UpdateStatus(message);
            
            // Nach 3 Sekunden zurücksetzen
            Invoke(nameof(ResetZone), 3f);
        }
        
        /// <summary>
        /// Zeigt Fehler-Feedback
        /// </summary>
        public void ShowError(string message = "✗ Leider falsch!")
        {
            isValidating = true;
            currentBaseColor = errorColor;
            if (zoneMaterial != null)
            {
                zoneMaterial.SetColor("_BaseColor", errorColor);
            }
            UpdateStatus(message);
            
            // Nach 3 Sekunden zurücksetzen
            Invoke(nameof(ResetZone), 3f);
        }
        
        /// <summary>
        /// Setzt die Zone zurück
        /// </summary>
        private void ResetZone()
        {
            isValidating = false;
            currentBaseColor = emptyColor;
        }
        
        /// <summary>
        /// Gibt alle zusammenhängenden Moleküle in der Zone zurück
        /// </summary>
        public List<List<Atom>> GetMoleculesInZone()
        {
            List<List<Atom>> molecules = new List<List<Atom>>();
            HashSet<Atom> processed = new HashSet<Atom>();
            
            foreach (var atom in atomsInZone)
            {
                if (processed.Contains(atom)) continue;
                
                // BFS um verbundene Atome zu finden
                List<Atom> molecule = new List<Atom>();
                Queue<Atom> queue = new Queue<Atom>();
                
                queue.Enqueue(atom);
                processed.Add(atom);
                molecule.Add(atom);
                
                while (queue.Count > 0)
                {
                    Atom current = queue.Dequeue();
                    
                    foreach (var connected in current.ConnectedAtoms)
                    {
                        if (!processed.Contains(connected))
                        {
                            processed.Add(connected);
                            molecule.Add(connected);
                            queue.Enqueue(connected);
                        }
                    }
                }
                
                molecules.Add(molecule);
            }
            
            return molecules;
        }
    }
}
