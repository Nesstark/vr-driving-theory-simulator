using UnityEngine.UI;
using UnityEngine;

[System.Serializable]
public class ButtonConfig
{
    [Header("VR Button Settings")]
    public Animator buttonAnimator;  // Instead of UI Button
    public bool isCorrectAnswer = false;
    
    [Header("Penalty Settings")]
    public int fineAmount = 0;
    public int penaltyPoints = 0;
    
    [HideInInspector]
    public bool isSelected = false;
}