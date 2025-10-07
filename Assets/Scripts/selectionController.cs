using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class SelectionController : MonoBehaviour
{
    [Header("Question Data")]
    [SerializeField] private int questionId = 0;
    [SerializeField] private string questionText = "";
    [SerializeField] private GameObject questionTextGameObject;
    
    [Header("Button Configuration")]
    [SerializeField] private ButtonConfig[] buttonConfigs = new ButtonConfig[4];
    
    [Header("General Settings")]
    [SerializeField] private bool allowMultipleSelections = false;
    [SerializeField] private float feedbackDuration = 2f;
    [SerializeField] private bool resetAfterSelection = true;
    
    [Header("Default Penalty (if no button-specific penalty is set)")]
    [SerializeField] private int defaultFineAmount = 250;
    [SerializeField] private int defaultPenaltyPoints = 0;
    
    [Header("Confirmation")]
    [SerializeField] private Button confirmButton;
    
    [Header("Navigation")]
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private string previousSceneName = "";
    [SerializeField] private string nextSceneName = "";
    
    [Header("Events")]
    public UnityEvent OnCorrectSelection;
    public UnityEvent OnIncorrectSelection;
    public UnityEvent OnSelectionComplete;
    public UnityEvent OnSelectionChanged;
    
    private bool selectionConfirmed = false;
    private int correctAnswerIndex = -1;
    
    void Start()
    {
        InitializeButtons();
        FindCorrectAnswer();
        RestorePreviousAnswers();
    }

    private void InitializeButtons()
    {
        SetQuestionText();
        
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].buttonGameObject == null) continue;

            if (buttonConfigs[i].textGameObject != null) {
                SetButtonText(i, buttonConfigs[i].buttonText);
            }
            
            SetButtonState(i, false);
        }

        if (confirmButton != null) {
            confirmButton.onClick.AddListener(ConfirmSelection);
        }

        SetupNavigationButtons();
    }

    private void FindCorrectAnswer()
    {
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (!buttonConfigs[i].isCorrectAnswer) continue;
            correctAnswerIndex = i;
            break;
        }
    }

    private void RestorePreviousAnswers()
    {
        var savedResult = QuestionResults.GetResult(questionId);
        if (savedResult == null) return;

        for (int i = 0; i < savedResult.selectedButtonIndices.Length; i++) {
            int buttonIndex = savedResult.selectedButtonIndices[i];
            if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) continue;
            
            buttonConfigs[buttonIndex].isSelected = true;
            SetButtonState(buttonIndex, true);
        }

        if (savedResult.wasConfirmed) {
            selectionConfirmed = true;
            Debug.Log($"[Question {questionId}] Previous answer restored: {(savedResult.wasAnsweredCorrectly ? "Correct" : "Incorrect")}");
        }
    }

    public void ToggleButtonSelection(int buttonIndex)
    {
        if (selectionConfirmed) {
            Debug.Log("Selection has already been confirmed");
            return;
        }
            
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) {
            Debug.LogWarning($"Invalid button index: {buttonIndex}");
            return;
        }

        ButtonConfig button = buttonConfigs[buttonIndex];
        if (button.buttonGameObject == null) {
            Debug.LogWarning($"Button GameObject is not assigned for button index: {buttonIndex}");
            return;
        }

        if (!allowMultipleSelections && !button.isSelected) {
            Debug.Log("Clearing other selections due to single-selection mode");
            ClearAllSelections();
        }
        
        button.isSelected = !button.isSelected;
        SetButtonState(buttonIndex, button.isSelected);
        OnSelectionChanged?.Invoke();
    }

    private void SetButtonState(int buttonIndex, bool isSelected)
    {
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) {
            Debug.LogWarning($"Invalid button index: {buttonIndex}");
            return;
        }
        
        if (buttonConfigs[buttonIndex].buttonGameObject == null) {
            Debug.LogWarning($"Button GameObject is not assigned for button index: {buttonIndex}");
            return;
        }

        Debug.Log($"Setting button {buttonIndex} state to: {isSelected}");

        // Look for Animator on the buttonGameObject itself or its children
        var animator = buttonConfigs[buttonIndex].buttonGameObject.GetComponentInChildren<Animator>();
        if (animator != null) {
            animator.SetBool("isPressed", isSelected);
            Debug.Log($"Animator found and isPressed set to {isSelected} for button {buttonIndex}");
        } else {
            Debug.LogWarning($"No Animator component found on button {buttonIndex} or its children");
        }
        
        // Look for VRButtonInteractor on the buttonGameObject itself or its children
        var vrButtonInteractor = buttonConfigs[buttonIndex].buttonGameObject.GetComponentInChildren<VRButtonInteractor>();
        if (vrButtonInteractor != null) {
            vrButtonInteractor.SetButtonPressed(isSelected);
            Debug.Log($"VRButtonInteractor found and SetButtonPressed({isSelected}) called for button {buttonIndex}");
        } else {
            Debug.LogWarning($"No VRButtonInteractor component found on button {buttonIndex} or its children");
        }
    }

    private void SetButtonText(int buttonIndex, string text)
    {
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) return;
        if (buttonConfigs[buttonIndex].textGameObject == null) return;
        if (string.IsNullOrEmpty(text)) return;
        
        var textComponent = buttonConfigs[buttonIndex].textGameObject.GetComponentInChildren<Text>();
        if (textComponent != null) {
            textComponent.text = text;
            return;
        }
        
        var tmpTextComponent = buttonConfigs[buttonIndex].textGameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpTextComponent != null) {
            tmpTextComponent.text = text;
            return;
        }
        
        Debug.LogWarning($"No Text or TextMeshProUGUI component found on text GameObject for button {buttonIndex} ({buttonConfigs[buttonIndex].textGameObject.name}). Text '{text}' could not be set.");
    }

    private void ClearAllSelections()
    {
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (!buttonConfigs[i].isSelected) continue;
            
            buttonConfigs[i].isSelected = false;
            SetButtonState(i, false);
        }
    }

    public void ConfirmSelection()
    {
        if (selectionConfirmed) return;
        
        bool answeredCorrectly = CheckIfAnsweredCorrectly();
        LogAnswerCheck();
        SaveCurrentResult(answeredCorrectly, false);
        
        if (answeredCorrectly) {
            Debug.Log($"[Question {questionId}] Answered correctly!");
            OnCorrectSelection?.Invoke();
        } else {
            Debug.Log($"[Question {questionId}] Answered incorrectly.");
            OnIncorrectSelection?.Invoke();
        }
        
        selectionConfirmed = true;
        OnSelectionComplete?.Invoke();
        
        if (resetAfterSelection) {
            Invoke(nameof(ResetSelection), feedbackDuration);
        }
    }

    private bool CheckIfAnsweredCorrectly()
    {
        int totalCorrectAnswers = 0;
        int selectedCorrectAnswers = 0;
        int selectedIncorrectAnswers = 0;
        
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isCorrectAnswer) {
                totalCorrectAnswers++;
                if (buttonConfigs[i].isSelected) {
                    selectedCorrectAnswers++;
                }
            } else if (buttonConfigs[i].isSelected) {
                selectedIncorrectAnswers++;
            }
        }
        
        bool allCorrectSelected = (selectedCorrectAnswers == totalCorrectAnswers);
        bool noIncorrectSelected = (selectedIncorrectAnswers == 0);
        
        return allCorrectSelected && noIncorrectSelected;
    }

    private void LogAnswerCheck()
    {
        int totalCorrectAnswers = 0;
        int selectedCorrectAnswers = 0;
        int selectedIncorrectAnswers = 0;
        
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isCorrectAnswer) {
                totalCorrectAnswers++;
                if (buttonConfigs[i].isSelected) {
                    selectedCorrectAnswers++;
                }
            } else if (buttonConfigs[i].isSelected) {
                selectedIncorrectAnswers++;
            }
        }
        
        Debug.Log($"[Question {questionId}] Answer check: {selectedCorrectAnswers}/{totalCorrectAnswers} correct selected, {selectedIncorrectAnswers} incorrect selected");
    }

    public void ResetSelection()
    {
        selectionConfirmed = false;
        ClearAllSelections();
    }

    private void SaveCurrentResult(bool answeredCorrectly, bool applyPenalties = true)
    {
        var result = new QuestionResult(questionId);
        
        var selectedIndices = new List<int>();
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isSelected) {
                selectedIndices.Add(i);
            }
        }
        result.selectedButtonIndices = selectedIndices.ToArray();
        
        var correctIndices = new List<int>();
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isCorrectAnswer) {
                correctIndices.Add(i);
            }
        }
        result.correctButtonIndices = correctIndices.ToArray();
        
        for (int i = 0; i < buttonConfigs.Length; i++) {
            result.buttonTexts[i] = GetButtonText(i);
        }
        
        result.wasAnsweredCorrectly = answeredCorrectly;
        result.wasConfirmed = selectionConfirmed;
        
        int totalFine = 0;
        int totalPoints = 0;
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (!buttonConfigs[i].isSelected || buttonConfigs[i].isCorrectAnswer) continue;
            
            int fine = buttonConfigs[i].fineAmount > 0 ? buttonConfigs[i].fineAmount : defaultFineAmount;
            int points = buttonConfigs[i].penaltyPoints > 0 ? buttonConfigs[i].penaltyPoints : defaultPenaltyPoints;
            totalFine += fine;
            totalPoints += points;
        }
        result.penaltyFineApplied = totalFine;
        result.penaltyPointsApplied = totalPoints;
        
        if (applyPenalties && !answeredCorrectly) {
            if (totalPoints > 0) {
                PenaltyTracker.AddPenalty(totalFine, totalPoints);
            } else {
                PenaltyTracker.AddPenalty(totalFine);
            }
            Debug.Log($"[Question {questionId}] Penalty applied: ${totalFine} fine, {totalPoints} points");
        }
        
        QuestionResults.SaveResult(result);
    }

    private string GetButtonText(int buttonIndex)
    {
        if (buttonConfigs[buttonIndex].textGameObject == null) {
            return $"Button {buttonIndex + 1}";
        }
        
        var textComponent = buttonConfigs[buttonIndex].textGameObject.GetComponentInChildren<Text>();
        if (textComponent != null) {
            return textComponent.text;
        }
        
        var tmpTextComponent = buttonConfigs[buttonIndex].textGameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpTextComponent != null) {
            return tmpTextComponent.text;
        }
        
        return $"Button {buttonIndex + 1}";
    }

    public void SaveCurrentSelections()
    {
        bool answeredCorrectly = CheckIfAnsweredCorrectly();
        SaveCurrentResult(answeredCorrectly, false);
        Debug.Log($"Saved selections for question {questionId}");
    }

    private void SetupNavigationButtons()
    {
        if (previousButton != null) {
            if (string.IsNullOrEmpty(previousSceneName)) {
                previousButton.interactable = false;
            } else {
                previousButton.onClick.AddListener(() => SaveAndNavigateToPrevious(previousSceneName));
                previousButton.interactable = true;
            }
        }
        
        if (nextButton != null) {
            if (string.IsNullOrEmpty(nextSceneName)) {
                nextButton.interactable = false;
            } else {
                nextButton.onClick.AddListener(() => SaveAndNavigateToNext(nextSceneName));
                nextButton.interactable = true;
            }
        }
    }

    public void SaveAndNavigateToNext(string nextSceneName = "")
    {
        SaveCurrentSelections();
        
        if (string.IsNullOrEmpty(nextSceneName)) {
            Debug.LogWarning("Next scene name not provided. Navigation cancelled.");
            return;
        }
        
        Debug.Log($"Navigating to: {nextSceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }
    
    public void SaveAndNavigateToPrevious(string previousSceneName = "")
    {
        SaveCurrentSelections();
        
        if (string.IsNullOrEmpty(previousSceneName)) {
            Debug.LogWarning("Previous scene name not provided. Navigation cancelled.");
            return;
        }
        
        Debug.Log($"Navigating to: {previousSceneName}");
        UnityEngine.SceneManagement.SceneManager.LoadScene(previousSceneName);
    }

    private bool SceneExistsInBuild(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName) return true;
        }
        return false;
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
    
    public void SetQuestionId(int id)
    {
        questionId = id;
    }
    
    public int GetQuestionId()
    {
        return questionId;
    }
    
    public void SetQuestionText(string text = "")
    {
        if (!string.IsNullOrEmpty(text)) {
            questionText = text;
        }
        
        if (questionTextGameObject == null) return;
        if (string.IsNullOrEmpty(questionText)) return;
        
        var textComponent = questionTextGameObject.GetComponentInChildren<Text>();
        if (textComponent != null) {
            textComponent.text = questionText;
            return;
        }
        
        var tmpTextComponent = questionTextGameObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (tmpTextComponent != null) {
            tmpTextComponent.text = questionText;
            return;
        }
        
        Debug.LogWarning($"No Text or TextMeshProUGUI component found on question text GameObject ({questionTextGameObject.name}). Question text '{questionText}' could not be set.");
    }
    
    public string GetQuestionText()
    {
        return questionText;
    }

    public bool HasSelectionBeenMade()
    {
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isSelected) return true;
        }
        return false;
    }
    
    public bool WasAnswered()
    {
        var result = QuestionResults.GetResult(questionId);
        return result != null && result.wasConfirmed;
    }
    
    public bool WasAnsweredCorrectly()
    {
        var result = QuestionResults.GetResult(questionId);
        return result != null && result.wasConfirmed && result.wasAnsweredCorrectly;
    }

    private void ApplyPenalty(int buttonIndex)
    {
        ButtonConfig incorrectButton = buttonConfigs[buttonIndex];
        
        int fineToApply = incorrectButton.fineAmount > 0 ? incorrectButton.fineAmount : defaultFineAmount;
        int pointsToApply = incorrectButton.penaltyPoints > 0 ? incorrectButton.penaltyPoints : defaultPenaltyPoints;
        
        if (pointsToApply > 0) {
            PenaltyTracker.AddPenalty(fineToApply, pointsToApply);
        } else {
            PenaltyTracker.AddPenalty(fineToApply);
        }
        
        Debug.Log($"Penalty applied: ${fineToApply} fine, {pointsToApply} points");
        Debug.Log(PenaltyTracker.GetPenaltySummary());
    }

    public static void FinalConfirmAllAnswers(string resultsSceneName = "Results")
    {
        var allResults = QuestionResults.GetAllResults();
        int totalFine = 0;
        int totalPoints = 0;
        
        Debug.Log("[FINAL] Answer check for all questions:");
        
        foreach (var result in allResults) {
            string status = result.wasAnsweredCorrectly ? "✅ Correct" : "❌ Incorrect";
            Debug.Log($"  Question {result.questionId}: {status}");
            
            if (!result.wasAnsweredCorrectly) {
                totalFine += result.penaltyFineApplied;
                totalPoints += result.penaltyPointsApplied;
            }
        }
        
        if (totalPoints > 0) {
            PenaltyTracker.AddPenalty(totalFine, totalPoints);
        } else if (totalFine > 0) {
            PenaltyTracker.AddPenalty(totalFine);
        }
        
        Debug.Log($"[FINAL] Test completed! Total penalties: ${totalFine} fine, {totalPoints} points");
        
        QuestionResults.MarkAllAsConfirmed();
        UnityEngine.SceneManagement.SceneManager.LoadScene(resultsSceneName);
    }
}