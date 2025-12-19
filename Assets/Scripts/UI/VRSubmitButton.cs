using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.Events;
using TMPro;

namespace InsideMatter.UI
{
    /// <summary>
    /// VR-freundlicher 3D-Button zum Drücken.
    /// Designed für VR Best Practices: Groß, gut sichtbar, haptisches Feedback.
    /// </summary>
    public class VRSubmitButton : MonoBehaviour
    {
        [Header("Button Einstellungen")]
        [Tooltip("Text auf dem Button")]
        public string buttonText = "PRÜFEN ✓";
        
        [Tooltip("Größe des Buttons")]
        public float buttonSize = 0.15f;
        
        [Tooltip("Höhe des Buttons")]
        public float buttonHeight = 0.05f;
        
        [Header("Farben")]
        public Color normalColor = new Color(0.2f, 0.7f, 0.3f); // Grün
        public Color hoverColor = new Color(0.3f, 0.9f, 0.4f);
        public Color pressedColor = new Color(0.1f, 0.5f, 0.2f);
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f);
        
        [Header("Animation")]
        [Tooltip("Wie weit der Button eingedrückt wird")]
        public float pressDepth = 0.02f;
        
        [Tooltip("Geschwindigkeit der Animation")]
        public float animationSpeed = 10f;
        
        [Header("Haptic Feedback")]
        [Range(0f, 1f)]
        public float hapticIntensity = 0.5f;
        public float hapticDuration = 0.1f;
        
        [Header("Events")]
        public UnityEvent OnButtonPressed;
        
        // Komponenten
        private Transform buttonTop;
        private MeshRenderer buttonRenderer;
        private Material buttonMaterial;
        private TextMeshPro buttonLabel;
        private XRSimpleInteractable interactable;
        
        private Vector3 originalPosition;
        private Vector3 pressedPosition;
        private bool isPressed = false;
        private bool isHovered = false;
        private bool isEnabled = true;
        
        void Awake()
        {
            CreateButtonVisual();
            SetupInteraction();
        }
        
        void Update()
        {
            // Smooth animation
            Vector3 targetPos = isPressed ? pressedPosition : originalPosition;
            if (buttonTop != null)
            {
                buttonTop.localPosition = Vector3.Lerp(
                    buttonTop.localPosition, 
                    targetPos, 
                    Time.deltaTime * animationSpeed
                );
            }
            
            // Farb-Update
            UpdateColor();
        }
        
        /// <summary>
        /// Erstellt die visuelle Darstellung des Buttons
        /// </summary>
        private void CreateButtonVisual()
        {
            // Basis (Sockel)
            GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "ButtonBase";
            baseObj.transform.SetParent(transform);
            baseObj.transform.localPosition = Vector3.zero;
            baseObj.transform.localScale = new Vector3(buttonSize * 1.2f, buttonHeight * 0.3f, buttonSize * 1.2f);
            
            var baseRenderer = baseObj.GetComponent<MeshRenderer>();
            var baseMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            baseMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.3f));
            baseRenderer.material = baseMat;
            
            // Collider entfernen (kommt auf Parent)
            Destroy(baseObj.GetComponent<Collider>());
            
            // Button-Oberseite (drückbar)
            GameObject topObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            topObj.name = "ButtonTop";
            topObj.transform.SetParent(transform);
            topObj.transform.localPosition = new Vector3(0, buttonHeight * 0.5f, 0);
            topObj.transform.localScale = new Vector3(buttonSize, buttonHeight * 0.5f, buttonSize);
            
            buttonTop = topObj.transform;
            originalPosition = buttonTop.localPosition;
            pressedPosition = originalPosition - new Vector3(0, pressDepth, 0);
            
            buttonRenderer = topObj.GetComponent<MeshRenderer>();
            buttonMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            buttonMaterial.SetColor("_BaseColor", normalColor);
            buttonRenderer.material = buttonMaterial;
            
            // Collider entfernen
            Destroy(topObj.GetComponent<Collider>());
            
            // Text-Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(buttonTop);
            labelObj.transform.localPosition = new Vector3(0, buttonHeight * 0.6f, 0);
            labelObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
            
            buttonLabel = labelObj.AddComponent<TextMeshPro>();
            buttonLabel.text = buttonText;
            buttonLabel.fontSize = 1.5f;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = Color.white;
            
            RectTransform rect = labelObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(buttonSize * 6f, buttonSize * 2f);
            
            // Haupt-Collider für Interaktion
            BoxCollider col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(buttonSize * 1.2f, buttonHeight, buttonSize * 1.2f);
            col.center = new Vector3(0, buttonHeight * 0.5f, 0);
        }
        
        /// <summary>
        /// Richtet XR Interaction ein
        /// </summary>
        private void SetupInteraction()
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
            
            interactable.selectEntered.AddListener(OnSelect);
            interactable.selectExited.AddListener(OnDeselect);
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
        
        private void OnSelect(SelectEnterEventArgs args)
        {
            if (!isEnabled) return;
            
            isPressed = true;
            
            // Haptic Feedback
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controller)
            {
                controller.SendHapticImpulse(hapticIntensity, hapticDuration);
            }
            
            // Event auslösen
            OnButtonPressed?.Invoke();
            
            Debug.Log("[VRSubmitButton] Button pressed!");
        }
        
        private void OnDeselect(SelectExitEventArgs args)
        {
            isPressed = false;
        }
        
        private void OnHoverEnter(HoverEnterEventArgs args)
        {
            isHovered = true;
            
            // Leichtes Haptic Feedback beim Hover
            if (args.interactorObject is UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controller)
            {
                controller.SendHapticImpulse(hapticIntensity * 0.3f, hapticDuration * 0.5f);
            }
        }
        
        private void OnHoverExit(HoverExitEventArgs args)
        {
            isHovered = false;
        }
        
        /// <summary>
        /// Aktualisiert die Button-Farbe
        /// </summary>
        private void UpdateColor()
        {
            if (buttonMaterial == null) return;
            
            Color targetColor;
            
            if (!isEnabled)
                targetColor = disabledColor;
            else if (isPressed)
                targetColor = pressedColor;
            else if (isHovered)
                targetColor = hoverColor;
            else
                targetColor = normalColor;
            
            Color currentColor = buttonMaterial.GetColor("_BaseColor");
            buttonMaterial.SetColor("_BaseColor", Color.Lerp(currentColor, targetColor, Time.deltaTime * animationSpeed));
        }
        
        /// <summary>
        /// Aktiviert/Deaktiviert den Button
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            if (interactable != null)
            {
                interactable.enabled = enabled;
            }
        }
        
        /// <summary>
        /// Setzt den Button-Text
        /// </summary>
        public void SetText(string text)
        {
            buttonText = text;
            if (buttonLabel != null)
            {
                buttonLabel.text = text;
            }
        }
        
        void OnDestroy()
        {
            if (interactable != null)
            {
                interactable.selectEntered.RemoveListener(OnSelect);
                interactable.selectExited.RemoveListener(OnDeselect);
                interactable.hoverEntered.RemoveListener(OnHoverEnter);
                interactable.hoverExited.RemoveListener(OnHoverExit);
            }
        }
    }
}
