using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class PushButton : MonoBehaviour
{
    [Header("Button Settings")]
    [Tooltip("Wie stark der Button beim Drücken in Y skaliert wird")]
    [Range(0.1f, 1f)]
    public float pressedScaleY = 0.5f;

    [Header("Move Target")]
    [Tooltip("Dieses Objekt wird an seine eigene Startposition und -rotation gesetzt, wenn der Button gedrückt wird.")]
    public GameObject targetObject;

    [Header("Button Action")]
    public UnityEvent onButtonPressed;

    private Vector3 originalScale;
    private Vector3 savedTargetPosition;
    private Quaternion savedTargetRotation; // Speichert die Startrotation
    private bool isPressed = false;
    private XRSimpleInteractable interactable;

    private void Awake()
    {
        originalScale = transform.localScale;
        interactable = GetComponent<XRSimpleInteractable>();

        // Speichere die Transformation des Zielobjekts beim Start
        if (targetObject != null)
        {
            savedTargetPosition = targetObject.transform.position;
            savedTargetRotation = targetObject.transform.rotation;
        }
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(HandleXRSelectEntered);
            interactable.selectExited.AddListener(HandleXRSelectExited);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(HandleXRSelectEntered);
            interactable.selectExited.RemoveListener(HandleXRSelectExited);
        }
    }

    private void HandleXRSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"<color=green>XR Select Entered auf {gameObject.name}</color>");
        Press();
    }

    private void HandleXRSelectExited(SelectExitEventArgs args) => Release();

    public void Press()
    {
        if (isPressed) return;
        isPressed = true;

        // Visuelles Feedback für den Button
        transform.localScale = new Vector3(
            originalScale.x,
            originalScale.y * pressedScaleY,
            originalScale.z
        );

        // Zielobjekt zurücksetzen
        if (targetObject != null)
        {
            // 1. Position und Rotation setzen
            targetObject.transform.position = savedTargetPosition;
            targetObject.transform.rotation = savedTargetRotation;

            // 2. Physikalische Bewegung stoppen (wichtig für schwebende Objekte)
            if (targetObject.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log($"{targetObject.name} zurückgesetzt auf Position: {savedTargetPosition} und Rotation: {savedTargetRotation.eulerAngles}");
        }

        onButtonPressed?.Invoke();
    }

    public void Release()
    {
        if (!isPressed) return;
        isPressed = false;
        transform.localScale = originalScale;
    }
}