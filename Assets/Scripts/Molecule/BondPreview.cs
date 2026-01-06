using UnityEngine;
using TMPro;

namespace InsideMatter.Molecule
{
    /// <summary>
    /// Zeigt eine Vorschau-Linie zwischen zwei BondPoints an, bevor die Bindung gefestigt wird.
    /// Die Vorschau-Linie ist transparent und wird beim Loslassen zur festen Bindung.
    /// </summary>
    public class BondPreview : MonoBehaviour
    {
        public static BondPreview Instance { get; private set; }

        [Header("Preview Settings")]
        [Tooltip("Maximaler Abstand für Vorschau-Anzeige")]
        public float previewDistance = 0.4f;

        [Tooltip("Transparenz der Vorschau-Linie (0-1)")]
        [Range(0f, 1f)]
        public float previewAlpha = 0.6f;

        [Tooltip("Farbe der Vorschau-Linie")]
        public Color previewColor = Color.yellow;

        [Tooltip("Dicke der Vorschau-Linie (sollte gleich wie bondThickness im MoleculeManager sein)")]
        public float previewThickness = 0.05f;
        
        [Header("Hint Settings")]
        [Tooltip("Text der angezeigt wird (leer = kein Text)")]
        public string hintTextContent = ""; // Deaktiviert
        public float hintVerticalOffset = 0.15f;
        public float hintFontSize = 1.5f;

        [Header("Audio")]
        [Tooltip("Sound beim Festigen der Bindung")]
        public AudioClip bondCreateSound;

        [Tooltip("Sound beim Lösen der Bindung")]
        public AudioClip bondBreakSound;

        [Tooltip("Lautstärke der Bond-Sounds")]
        public float soundVolume = 0.7f;

        // Aktuelle Vorschau-Daten
        private BondPoint previewBondPointA;
        private BondPoint previewBondPointB;
        private GameObject previewVisual;
        private BondVisual previewBondVisual;
        private Material previewMaterial;
        private AudioSource audioSource;
        
        // Visual Hint
        private TextMeshPro hintText;

        // Ist eine Vorschau aktiv?
        public bool IsPreviewActive => previewBondPointA != null && previewBondPointB != null;
        public BondPoint PreviewBondPointA => previewBondPointA;
        public BondPoint PreviewBondPointB => previewBondPointB;
        
        // Aktueller Bond-Typ für die Preview
        private BondType previewBondType = BondType.Single;
        public BondType CurrentBondType => previewBondType;
        
        /// <summary>
        /// Wechselt zum nächsten Bond-Typ (Single -> Double -> Triple -> Single)
        /// </summary>
        public void CycleBondType()
        {
            switch (previewBondType)
            {
                case BondType.Single:
                    previewBondType = BondType.Double;
                    break;
                case BondType.Double:
                    previewBondType = BondType.Triple;
                    break;
                case BondType.Triple:
                default:
                    previewBondType = BondType.Single;
                    break;
            }
            Debug.Log($"[BondPreview] Bond-Typ gewechselt zu: {previewBondType}");
            
            // Visual aktualisieren
            if (IsPreviewActive)
            {
                UpdatePreviewVisual();
            }
        }
        
        /// <summary>
        /// Setzt den Bond-Typ direkt
        /// </summary>
        public void SetBondType(BondType type)
        {
            previewBondType = type;
            if (IsPreviewActive)
            {
                UpdatePreviewVisual();
            }
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Mehrere BondPreview gefunden! Zerstöre Duplikat.");
                Destroy(gameObject);
                return;
            }

            // AudioSource erstellen
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D-Sound

            CreatePreviewVisual();
        }

        /// <summary>
        /// Erstellt das visuelle Preview-Objekt mit BondVisual für multi-line Support
        /// </summary>
        private void CreatePreviewVisual()
        {
            // Container für die Preview erstellen
            previewVisual = new GameObject("BondPreview");
            previewVisual.transform.SetParent(transform);
            
            // BondVisual Component hinzufügen für Single/Double/Triple Support
            previewBondVisual = previewVisual.AddComponent<BondVisual>();

            // Material mit Transparenz erstellen
            Shader shader = (MoleculeManager.Instance != null && MoleculeManager.Instance.bondMaterial != null) 
                ? MoleculeManager.Instance.bondMaterial.shader 
                : Shader.Find("Universal Render Pipeline/Lit");
            previewMaterial = new Material(shader);
            
            // Transparenz aktivieren
            previewMaterial.SetFloat("_Surface", 1); // Transparent
            previewMaterial.SetFloat("_Blend", 0);   // Alpha
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            previewMaterial.renderQueue = 3000;

            UpdatePreviewColor();

            // Anfangs verstecken
            previewVisual.SetActive(false);
            
            // --- Hint Text erstellen ---
            GameObject hintObj = new GameObject("HintText");
            hintObj.transform.SetParent(transform); // Nicht an previewVisual parenten, um Skalierung zu vermeiden
            hintText = hintObj.AddComponent<TextMeshPro>();
            hintText.text = hintTextContent;
            hintText.fontSize = hintFontSize;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.color = Color.white;
            // Outline für bessere Lesbarkeit
            hintText.outlineWidth = 0.2f;
            hintText.outlineColor = Color.black;
            
            hintObj.SetActive(false);
        }

        /// <summary>
        /// Aktualisiert die Farbe des Preview-Materials
        /// </summary>
        private void UpdatePreviewColor()
        {
            if (previewMaterial != null)
            {
                Color colorWithAlpha = previewColor;
                colorWithAlpha.a = previewAlpha;
                previewMaterial.SetColor("_BaseColor", colorWithAlpha);
                previewMaterial.SetColor("_Color", colorWithAlpha);
            }
        }

        void Update()
        {
            if (IsPreviewActive)
            {
                UpdatePreviewVisual();
            }
        }

        /// <summary>
        /// Zeigt eine Vorschau-Linie zwischen zwei BondPoints
        /// </summary>
        public void ShowPreview(BondPoint bondPointA, BondPoint bondPointB)
        {
            if (bondPointA == null || bondPointB == null) return;
            if (bondPointA.Occupied || bondPointB.Occupied) return;
            if (bondPointA.ParentAtom == bondPointB.ParentAtom) return;

            previewBondPointA = bondPointA;
            previewBondPointB = bondPointB;

            previewVisual.SetActive(true);
            if (hintText != null) hintText.gameObject.SetActive(true);
            
            UpdatePreviewVisual();

            if (MoleculeManager.Instance != null && MoleculeManager.Instance.debugMode)
            {
                Debug.Log($"Preview: {bondPointA.ParentAtom.element} <-> {bondPointB.ParentAtom.element}");
            }
        }

        /// <summary>
        /// Versteckt die aktuelle Vorschau
        /// </summary>
        public void HidePreview()
        {
            previewBondPointA = null;
            previewBondPointB = null;
            previewVisual.SetActive(false);
            if (hintText != null) hintText.gameObject.SetActive(false);
        }

        /// <summary>
        /// Aktualisiert Position und Rotation der Vorschau-Linie
        /// </summary>
        private void UpdatePreviewVisual()
        {
            if (previewBondPointA == null || previewBondPointB == null) return;
            if (previewBondVisual == null) return;

            Vector3 posA = previewBondPointA.transform.position;
            Vector3 posB = previewBondPointB.transform.position;
            Vector3 center = (posA + posB) / 2f;

            // Position in der Mitte
            previewVisual.transform.position = center;

            // Rotation zur Verbindung der Punkte
            Vector3 direction = posB - posA;
            float distance = direction.magnitude;

            if (distance > 0.001f)
            {
                previewVisual.transform.up = direction.normalized;
            }

            // BondVisual für Single/Double/Triple Visualisierung nutzen
            previewBondVisual.UpdateVisuals(previewBondType, distance, previewThickness, previewMaterial);
            
            // --- Update Hint Position ---
            if (hintText != null)
            {
                hintText.transform.position = center + Vector3.up * hintVerticalOffset;
                
                // Zum User/Kamera drehen
                if (Camera.main != null)
                {
                    hintText.transform.rotation = Quaternion.LookRotation(hintText.transform.position - Camera.main.transform.position);
                }
            }
        }

        /// <summary>
        /// Festigt die aktuelle Vorschau als echte Bindung
        /// </summary>
        public bool ConfirmBond()
        {
            if (!IsPreviewActive) return false;

            BondPoint bpA = previewBondPointA;
            BondPoint bpB = previewBondPointB;
            BondType bondType = previewBondType; // Aktuellen Typ speichern

            // Vorschau verstecken
            HidePreview();
            
            // Bond-Typ NICHT zurücksetzen - Auswahl bleibt für nächste Bindung erhalten

            // Echte Bindung erstellen mit gewähltem Typ
            if (MoleculeManager.Instance != null)
            {
                MoleculeManager.Instance.CreateBond(bpA, bpB, bondType);
                PlayBondSound(bondCreateSound, bpA.transform.position);
                Debug.Log($"[BondPreview] Bond erstellt mit Typ: {bondType}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Spielt einen Bond-Sound ab
        /// </summary>
        public void PlayBondSound(AudioClip clip, Vector3 position)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.transform.position = position;
                audioSource.PlayOneShot(clip, soundVolume);
            }
        }

        /// <summary>
        /// Spielt den Bond-Break-Sound ab
        /// </summary>
        public void PlayBreakSound(Vector3 position)
        {
            PlayBondSound(bondBreakSound, position);
        }

        /// <summary>
        /// Findet den nächsten freien BondPoint eines Atoms zu einem Zielpunkt
        /// </summary>
        public static BondPoint FindClosestFreeBondPoint(Atom atom, Vector3 targetPosition)
        {
            if (atom == null) return null;

            BondPoint closest = null;
            float closestDistance = float.MaxValue;

            foreach (var bp in atom.bondPoints)
            {
                if (bp != null && !bp.Occupied)
                {
                    float dist = Vector3.Distance(bp.transform.position, targetPosition);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closest = bp;
                    }
                }
            }

            return closest;
        }

        /// <summary>
        /// Findet das beste BondPoint-Paar zwischen zwei Atomen
        /// </summary>
        public static (BondPoint, BondPoint) FindBestBondPointPair(Atom atomA, Atom atomB)
        {
            if (atomA == null || atomB == null) return (null, null);

            BondPoint bestA = null;
            BondPoint bestB = null;
            float bestDistance = float.MaxValue;

            foreach (var bpA in atomA.bondPoints)
            {
                if (bpA == null || bpA.Occupied) continue;

                foreach (var bpB in atomB.bondPoints)
                {
                    if (bpB == null || bpB.Occupied) continue;

                    float dist = Vector3.Distance(bpA.transform.position, bpB.transform.position);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestA = bpA;
                        bestB = bpB;
                    }
                }
            }

            return (bestA, bestB);
        }

        /// <summary>
        /// Findet den nächsten freien BondPoint nach Rotation (zyklisch durch alle freien BondPoints)
        /// </summary>
        public static BondPoint GetNextFreeBondPoint(Atom atom, BondPoint currentBondPoint)
        {
            if (atom == null) return null;

            var freeBondPoints = atom.bondPoints.FindAll(bp => bp != null && !bp.Occupied);
            if (freeBondPoints.Count == 0) return null;
            if (freeBondPoints.Count == 1) return freeBondPoints[0];

            int currentIndex = freeBondPoints.IndexOf(currentBondPoint);
            if (currentIndex < 0) return freeBondPoints[0];

            int nextIndex = (currentIndex + 1) % freeBondPoints.Count;
            return freeBondPoints[nextIndex];
        }
    }
}
