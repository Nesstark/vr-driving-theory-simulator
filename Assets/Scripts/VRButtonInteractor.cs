using UnityEngine;
using System.Collections.Generic;

public enum VRButtonType
{
    Answer,      // For answer selection, uses buttonIndex 0-3
    Navigation,  // For next/previous scene navigation
    UIToggle,    // For show/hide UI elements
    Confirm,     // For confirming answers
    Custom       // For custom functionality
}

public class VRButtonInteractor : MonoBehaviour
{
    [Header("Button Configuration")]
    [SerializeField] private VRButtonType buttonType = VRButtonType.Answer;
    [SerializeField] private int buttonIndex;
    [SerializeField] private SelectionController controller;
    
    [Header("Navigation Settings (for Navigation type)")]
    [SerializeField] private bool isNextButton = true; // true = next, false = previous
    
    [Header("Custom Settings (for Custom type)")]
    [SerializeField] private string customActionName = "";
    
    [Header("UI Toggle Settings (for UIToggle type)")]
    [SerializeField] private bool isUIVisible = true; // Track current UI state
    
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
            Debug.Log($"Button {buttonIndex} ({buttonType}): 3D renderer found, color changes enabled");
        } else {
            Debug.LogWarning($"Button {buttonIndex} ({buttonType}): No Renderer component found. This button will work for interactions but won't change color");
        }
        
        // Log button configuration
        LogButtonConfiguration();
    }
    
    private void LogButtonConfiguration()
    {
        switch (buttonType) {
            case VRButtonType.Answer:
                Debug.Log($"Answer button {buttonIndex} configured");
                break;
            case VRButtonType.Navigation:
                Debug.Log($"Navigation button configured: {(isNextButton ? "Next" : "Previous")}");
                break;
            case VRButtonType.UIToggle:
                Debug.Log($"UI Toggle button configured");
                break;
            case VRButtonType.Confirm:
                Debug.Log($"Confirm button configured");
                break;
            case VRButtonType.Custom:
                Debug.Log($"Custom button configured: {customActionName}");
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collider entered: {other.name} with tag {other.tag}");
        if (!other.CompareTag("Hand") && !other.CompareTag("Controller")) return;
        
        ExecuteButtonAction();
        Debug.Log($"Button {buttonIndex} ({buttonType}) pressed.");
    }

    private void ExecuteButtonAction()
    {
        switch (buttonType) {
            case VRButtonType.Answer:
                if (controller != null) {
                    controller.ToggleButtonSelection(buttonIndex);
                }
                break;
                
            case VRButtonType.Navigation:
                if (controller != null) {
                    if (isNextButton) {
                        controller.SaveAndNavigateToNext();
                    } else {
                        controller.SaveAndNavigateToPrevious();
                    }
                }
                break;
                
            case VRButtonType.UIToggle:
                ToggleUIVisibility();
                break;
                
            case VRButtonType.Confirm:
                if (controller != null) {
                    controller.ConfirmSelection();
                }
                break;
                
            case VRButtonType.Custom:
                ExecuteCustomAction();
                break;
        }
    }
    
    private void ToggleUIVisibility()
    {
        if (controller == null) {
            Debug.LogWarning($"UI Toggle button {buttonIndex}: No SelectionController found");
            return;
        }
        
        if (isUIVisible) {
            // Currently visible, so hide (dropdown interface)
            controller.HideUIElements();
            isUIVisible = false;
            Debug.Log($"UI Toggle button {buttonIndex}: UI elements hidden (dropdown interface)");
        } else {
            // Currently hidden, so show (dropup interface)
            controller.ShowUIElements();
            isUIVisible = true;
            Debug.Log($"UI Toggle button {buttonIndex}: UI elements shown (dropup interface)");
        }
    }
    
    private void ExecuteCustomAction()
    {
        if (string.IsNullOrEmpty(customActionName)) {
            Debug.LogWarning($"Custom button {buttonIndex} has no action name specified");
            return;
        }
        
        Debug.Log($"Executing custom action: {customActionName}");
        // TODO: Implement custom action system (could use Unity Events or method reflection) if needed. Likely irrelevant
    }
    
    public void SetButtonPressed(bool isPressed)
    {
        if (buttonRenderer == null || buttonMaterial == null) return;
        
        Color targetColor = isPressed ? pressedColor : normalColor;
        buttonMaterial.color = targetColor;
        Debug.Log($"Button {buttonIndex} color changed to: {targetColor}");
    }
}