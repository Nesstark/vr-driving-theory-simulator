using UnityEngine;
using System.Collections.Generic;

public class VRButtonInteractor : MonoBehaviour
{
    [SerializeField] private int buttonIndex;
    [SerializeField] private SelectionController controller;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color pressedColor = Color.green;
    [SerializeField] private Color normalColor = Color.white;
    
    private Renderer buttonRenderer;
    private Material buttonMaterial;
    private Color originalColor;

    void Start()
    {
        if (controller == null)
            controller = FindAnyObjectByType<SelectionController>();
            
        // Get the renderer and store the original color
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null) {
            buttonMaterial = buttonRenderer.material;
            originalColor = buttonMaterial.color;
            normalColor = originalColor; // Use the original color as normal color
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collider entered: {other.name} with tag {other.tag}");
        if (other.CompareTag("Hand") || other.CompareTag("Controller")) {
            controller.ToggleButtonSelection(buttonIndex);
            Debug.Log($"Button {buttonIndex} pressed.");
        }
    }
    
    // Public method to change button color (called by SelectionController)
    public void SetButtonPressed(bool isPressed)
    {
        Debug.Log($"SetButtonPressed called with isPressed: {isPressed} on button {buttonIndex}");
        
        if (buttonRenderer == null) {
            Debug.LogWarning($"buttonRenderer is null on button {buttonIndex}");
            return;
        }
        
        if (buttonMaterial == null) {
            Debug.LogWarning($"buttonMaterial is null on button {buttonIndex}");
            return;
        }
        
        Color targetColor = isPressed ? pressedColor : normalColor;
        buttonMaterial.color = targetColor;
        Debug.Log($"Button {buttonIndex} color changed to: {targetColor}");
    }
}