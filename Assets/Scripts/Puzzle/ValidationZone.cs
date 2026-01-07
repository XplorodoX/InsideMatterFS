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
        [Tooltip("Automatisch Größe vom GameObject übernehmen (Collider/Renderer)")]
        public bool autoDetectSize = true;
        
        [Tooltip("Breite der Validierungszone (X-Achse) - wird überschrieben wenn autoDetectSize aktiv")]
        public float zoneWidth = 0.5f;
        
        [Tooltip("Tiefe der Validierungszone (Z-Achse) - wird überschrieben wenn autoDetectSize aktiv")]
        public float zoneDepth = 0.4f;
        
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
            // Automatisch Größe erkennen
            if (autoDetectSize)
            {
                DetectSizeFromGameObject();
            }
            
            CreateZoneVisual();
            currentBaseColor = emptyColor;
            UpdateStatus("Platziere dein Molekül hier");
        }
        
        /// <summary>
        /// Erkennt automatisch die Größe vom Collider oder Renderer des GameObjects
        /// </summary>
        private void DetectSizeFromGameObject()
        {
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool foundBounds = false;
            
            // Versuche vom Collider zu lesen
            Collider col = GetComponent<Collider>();
            if (col != null && !(col is SphereCollider) && !(col is CapsuleCollider))
            {
                bounds = col.bounds;
                foundBounds = true;
                
                // Alten Collider entfernen (wir erstellen unseren eigenen Trigger)
                Destroy(col);
            }
            
            // Falls kein Collider, versuche Renderer
            if (!foundBounds)
            {
                Renderer rend = GetComponent<Renderer>();
                if (rend == null) rend = GetComponentInChildren<Renderer>();
                
                if (rend != null)
                {
                    bounds = rend.bounds;
                    foundBounds = true;
                }
            }
            
            if (foundBounds)
            {
                // Größe übernehmen (etwas kleiner als das Objekt für saubere Visuals)
                zoneWidth = bounds.size.x * 0.9f;
                zoneDepth = bounds.size.z * 0.9f;
                
                Debug.Log($"[ValidationZone] Größe automatisch erkannt: {zoneWidth:F2} x {zoneDepth:F2}");
            }
            else
            {
                Debug.LogWarning("[ValidationZone] Keine Bounds gefunden - verwende manuelle Größe");
            }
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
            // === RECHTECKIGER RAND erstellen ===
            CreateRectangularBorder();
            
            // === Boden-Platte (sehr transparent) ===
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ZoneFloor";
            floor.transform.SetParent(transform);
            floor.transform.localPosition = new Vector3(0, 0.005f, 0);
            floor.transform.localScale = new Vector3(zoneWidth, 0.01f, zoneDepth);
            
            Collider floorCollider = floor.GetComponent<Collider>();
            if (floorCollider != null) Destroy(floorCollider);
            
            // Sehr transparentes Material für Boden
            MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
            Material floorMat = CreateTransparentMaterial(new Color(1f, 1f, 1f, 0.15f));
            floorRenderer.material = floorMat;
            
            // === Trigger-Collider für Erkennung (Box statt Sphere) ===
            BoxCollider trigger = gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(zoneWidth, zoneHeight, zoneDepth);
            trigger.center = new Vector3(0, zoneHeight / 2f, 0);
            
            // === Statustext erstellen ===
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
        
        /// <summary>
        /// Erstellt einen rechteckigen Rand um die Zone
        /// </summary>
        private void CreateRectangularBorder()
        {
            float borderThickness = 0.02f; // Dicke des Rands
            float borderHeight = 0.08f; // Höhe des Rands
            
            // 4 Seiten des Rechtecks
            // Vorne (Z+)
            CreateBorderEdge("Front", new Vector3(0, borderHeight / 2f, zoneDepth / 2f), 
                             new Vector3(zoneWidth, borderHeight, borderThickness));
            // Hinten (Z-)
            CreateBorderEdge("Back", new Vector3(0, borderHeight / 2f, -zoneDepth / 2f), 
                             new Vector3(zoneWidth, borderHeight, borderThickness));
            // Links (X-)
            CreateBorderEdge("Left", new Vector3(-zoneWidth / 2f, borderHeight / 2f, 0), 
                             new Vector3(borderThickness, borderHeight, zoneDepth));
            // Rechts (X+)
            CreateBorderEdge("Right", new Vector3(zoneWidth / 2f, borderHeight / 2f, 0), 
                             new Vector3(borderThickness, borderHeight, zoneDepth));
        }
        
        /// <summary>
        /// Erstellt eine Kante des rechteckigen Rands
        /// </summary>
        private void CreateBorderEdge(string name, Vector3 position, Vector3 scale)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = $"Border_{name}";
            edge.transform.SetParent(transform);
            edge.transform.localPosition = position;
            edge.transform.localScale = scale;
            
            // Collider entfernen
            Collider col = edge.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            // Material
            MeshRenderer rend = edge.GetComponent<MeshRenderer>();
            if (zoneRenderer == null) zoneRenderer = rend;
            zoneMaterial = CreateTransparentMaterial(emptyColor);
            rend.material = zoneMaterial;
        }
        
        /// <summary>
        /// Erstellt ein transparentes Material
        /// </summary>
        private Material CreateTransparentMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            
            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetColor("_EmissionColor", color * 0.5f); // Leichtes Glühen
            mat.EnableKeyword("_EMISSION");
            return mat;
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
