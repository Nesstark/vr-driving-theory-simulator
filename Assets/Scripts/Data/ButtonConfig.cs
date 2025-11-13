using UnityEngine.UI;
using UnityEngine;

[System.Serializable]
public class ButtonConfig
{
    [Header("VR Button Settings")]
    public GameObject textGameObject;      // The GameObject containing the text component
    public GameObject buttonGameObject;    // The GameObject with the physical button (VRButtonInteractor)
    public bool isCorrectAnswer = false;
    
    [Header("Answer Text")]
    public string buttonText = "";
    
    [Header("Penalty Settings")]
    public int fineAmount = 0;
    
    [HideInInspector]
    public bool isSelected = false;
}