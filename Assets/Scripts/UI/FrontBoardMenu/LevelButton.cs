using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class LevelButton : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnSelectEntered);
    }

    void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        string buttonText = GetComponentInChildren<TextMeshProUGUI>().text;
        string levelNmbr = buttonText.Replace("Level ", "");
        int level = int.Parse(levelNmbr);
        Debug.Log("Level Nummer: " + levelNmbr + " wurde gedrückt. Integer Wert: " + level);
    }
}
