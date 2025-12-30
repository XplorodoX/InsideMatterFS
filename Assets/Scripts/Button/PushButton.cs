using UnityEngine;
using UnityEngine.Events;

public class PushButton : MonoBehaviour
{
    [Header("Button Settings")]
    [Tooltip("Wie stark der Button beim Drücken in Y skaliert wird")]
    [Range(0.1f, 1f)]
    public float pressedScaleY = 0.5f;

    [Header("Move Target Settings")]
    [Tooltip("GameObject, das beim Button-Klick bewegt werden soll")]
    public GameObject targetObject;

    [Tooltip("Zielposition (X, Y, Z), auf die das GameObject gesetzt wird")]
    public Vector3 targetPosition;

    [Header("Button Action")]
    public UnityEvent onButtonPressed;

    private Vector3 originalScale;
    private bool isPressed = false;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// Wird aufgerufen, wenn der Button gedrückt wird
    /// </summary>
    public void Press()
    {
        if (isPressed)
            return;

        isPressed = true;

        // Visuelles Feedback
        transform.localScale = new Vector3(
            originalScale.x,
            originalScale.y * pressedScaleY,
            originalScale.z
        );

        // Zielobjekt bewegen
        if (targetObject != null)
        {
            targetObject.transform.position = targetPosition;
        }

        // Zusätzliche Button-Funktion auslösen
        onButtonPressed?.Invoke();
    }

    /// <summary>
    /// Wird aufgerufen, wenn der Button losgelassen wird
    /// </summary>
    public void Release()
    {
        if (!isPressed)
            return;

        isPressed = false;
        transform.localScale = originalScale;
    }
}