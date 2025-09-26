using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[System.Serializable]
public class ButtonConfig
{
    [Header("Button Settings")]
    public Button button;
    public bool isCorrectAnswer = false;
    
    [Header("Penalty Settings")]
    public int fineAmount = 0;
    public int penaltyPoints = 0;
}

public class selectionController : MonoBehaviour
{
    [Header("Button Configuration")]
    [SerializeField] private ButtonConfig[] buttonConfigs = new ButtonConfig[4];
    
    [Header("General Settings")]
    [SerializeField] private bool allowMultipleSelections = false;
    [SerializeField] private float feedbackDuration = 2f;
    [SerializeField] private bool resetAfterSelection = true;
    
    [Header("Default Penalty (if no button-specific penalty is set)")]
    [SerializeField] private int defaultFineAmount = 250;
    [SerializeField] private int defaultPenaltyPoints = 0;
    
    [Header("Events")]
    public UnityEvent OnCorrectSelection;
    public UnityEvent OnIncorrectSelection;
    public UnityEvent OnSelectionComplete;
    
    private bool selectionMade = false;
    private int correctAnswerIndex = -1;
    
    void Start()
    {
        InitializeButtons();
        FindCorrectAnswer();
    }
    
    private void InitializeButtons()
    {
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].button == null) continue;

            int index = i;
            buttonConfigs[i].button.onClick.AddListener(() => OnButtonSelected(index));
        }
    }
    
    private void FindCorrectAnswer()
    {
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (!buttonConfigs[i].isCorrectAnswer) continue;
            correctAnswerIndex = i;
            break;
        }
    }
    
    public void OnButtonSelected(int buttonIndex)
    {
        if (selectionMade && !allowMultipleSelections) return;
            
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) return;
            
        ButtonConfig selectedButton = buttonConfigs[buttonIndex];
        
        if (selectedButton.isCorrectAnswer) {
            HandleCorrectSelection(buttonIndex);
        }
        else {
            HandleIncorrectSelection(buttonIndex);
        }
        
        selectionMade = true;
        OnSelectionComplete?.Invoke();
        
        if (resetAfterSelection) {
            Invoke(nameof(ResetSelection), feedbackDuration);
        }
    }
    
    private void HandleCorrectSelection(int buttonIndex)
    {
        OnCorrectSelection?.Invoke();
    }
    
    private void HandleIncorrectSelection(int buttonIndex)
    {
        ButtonConfig incorrectButton = buttonConfigs[buttonIndex];
        
        int fineToApply = incorrectButton.fineAmount > 0 ? incorrectButton.fineAmount : defaultFineAmount;
        int pointsToApply = incorrectButton.penaltyPoints > 0 ? incorrectButton.penaltyPoints : defaultPenaltyPoints;
        
        if (pointsToApply > 0) {
            penaltyTracker.AddPenalty(fineToApply, pointsToApply);
        }
        else {
            penaltyTracker.AddPenalty(fineToApply);
        }
        
        Debug.Log($"Penalty applied: ${fineToApply} fine, {pointsToApply} points");
        Debug.Log(penaltyTracker.GetPenaltySummary());
        
        OnIncorrectSelection?.Invoke();
    }
    
    public void ResetSelection()
    {
        selectionMade = false;
    }
    
    public void SetCorrectAnswer(int buttonIndex)
    {
        if (correctAnswerIndex >= 0) {
            buttonConfigs[correctAnswerIndex].isCorrectAnswer = false;
        }
        
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) return;
        
        buttonConfigs[buttonIndex].isCorrectAnswer = true;
        correctAnswerIndex = buttonIndex;
    }
    
    public void SetButtonPenalty(int buttonIndex, int fineAmount, int penaltyPoints = 0)
    {
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) return;
        
        buttonConfigs[buttonIndex].fineAmount = fineAmount;
        buttonConfigs[buttonIndex].penaltyPoints = penaltyPoints;
    }
    
    public void SetDefaultPenalty(int fineAmount, int penaltyPoints = 0)
    {
        defaultFineAmount = fineAmount;
        defaultPenaltyPoints = penaltyPoints;
    }
    
    public bool HasSelectionBeenMade()
    {
        return selectionMade;
    }
    
    public int GetCorrectAnswerIndex()
    {
        return correctAnswerIndex;
    }
}