using UnityEngine.UI;
using UnityEngine;

[System.Serializable]
public class ButtonConfig
{
    [Header("Button Settings")]
    public Button button;
    public bool isCorrectAnswer = false;
    
    [Header("Penalty Settings")]
    public int fineAmount = 0;
    public int penaltyPoints = 0;
    
    [Header("Visual Feedback")]
    public Color defaultColor = Color.white;
    public Color selectedColor = new Color(0.7f, 0.9f, 1f, 1f); // Light blue
    
    [HideInInspector]
    public bool isSelected = false;
}