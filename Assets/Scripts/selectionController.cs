using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

public class SelectionController : MonoBehaviour
{
    [Header("Question Data")]
    [SerializeField] private int questionId = 0;
    [SerializeField] private string questionText = "";
    
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
        for (int i = 0; i < buttonConfigs.Length; i++)
        {
            if (buttonConfigs[i].buttonAnimator == null) continue;

            // Set default state through animator
            SetButtonState(i, false);
        }

        // Initialize confirm button (if using UI)
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(ConfirmSelection);
        }

        // Initialize navigation buttons
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
    
    public void ToggleButtonSelection(int buttonIndex)
    {
        if (selectionConfirmed) return;
            
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) return;
        
        ButtonConfig button = buttonConfigs[buttonIndex];
        if (button.buttonAnimator == null) return;
        
        // If not allowing multiple selections, clear other selections first
        if (!allowMultipleSelections && !button.isSelected) {
            ClearAllSelections();
        }
        
        button.isSelected = !button.isSelected;
        
        SetButtonState(buttonIndex, button.isSelected);
        
        OnSelectionChanged?.Invoke();
    }
    
    public void ConfirmSelection()
    {
        if (selectionConfirmed) return;
        
        bool answeredCorrectly = CheckIfAnsweredCorrectly();
        
        LogAnswerCheck();
        
        SaveCurrentResult(answeredCorrectly, false); // false = don't apply penalties
        
        if (answeredCorrectly) {
            Debug.Log($"[Question {questionId}] Answered correctly!");
            OnCorrectSelection?.Invoke();
        }
        else {
            Debug.Log($"[Question {questionId}] Answered incorrectly.");
            OnIncorrectSelection?.Invoke();
        }
        
        selectionConfirmed = true;
        OnSelectionComplete?.Invoke();
        
        if (resetAfterSelection) {
            Invoke(nameof(ResetSelection), feedbackDuration);
        }
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
            }
            else if (buttonConfigs[i].isSelected) {
                selectedIncorrectAnswers++;
            }
        }
        
        Debug.Log($"[Question {questionId}] Answer check: {selectedCorrectAnswers}/{totalCorrectAnswers} correct selected, {selectedIncorrectAnswers} incorrect selected");
    }
    
    private bool CheckIfAnsweredCorrectly()
    {
        // Count correct and incorrect answers
        int totalCorrectAnswers = 0;
        int selectedCorrectAnswers = 0;
        int selectedIncorrectAnswers = 0;
        
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isCorrectAnswer) {
                totalCorrectAnswers++;
                if (buttonConfigs[i].isSelected) {
                    selectedCorrectAnswers++;
                }
            }
            else if (buttonConfigs[i].isSelected) {
                selectedIncorrectAnswers++;
            }
        }
        
        // Must select ALL correct answers AND NO incorrect answers
        bool allCorrectSelected = (selectedCorrectAnswers == totalCorrectAnswers);
        bool noIncorrectSelected = (selectedIncorrectAnswers == 0);
        
        return allCorrectSelected && noIncorrectSelected;
    }
    
    private void ApplyPenalty(int buttonIndex)
    {
        ButtonConfig incorrectButton = buttonConfigs[buttonIndex];
        
        int fineToApply = incorrectButton.fineAmount > 0 ? incorrectButton.fineAmount : defaultFineAmount;
        int pointsToApply = incorrectButton.penaltyPoints > 0 ? incorrectButton.penaltyPoints : defaultPenaltyPoints;
        
        if (pointsToApply > 0) {
            PenaltyTracker.AddPenalty(fineToApply, pointsToApply);
        }
        else {
            PenaltyTracker.AddPenalty(fineToApply);
        }
        
        Debug.Log($"Penalty applied: ${fineToApply} fine, {pointsToApply} points");
        Debug.Log(PenaltyTracker.GetPenaltySummary());
    }
    
    private void SetButtonState(int buttonIndex, bool isSelected)
    {
        if (buttonIndex < 0 || buttonIndex >= buttonConfigs.Length) return;
        if (buttonConfigs[buttonIndex].buttonAnimator == null) return;
        
        // Set the animator's isPressed bool to control animation/color
        buttonConfigs[buttonIndex].buttonAnimator.SetBool("isPressed", isSelected);
    }
    
    private void ClearAllSelections()
    {
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isSelected) {
                buttonConfigs[i].isSelected = false;
                SetButtonState(i, false);
            }
        }
    }
    
    public void ResetSelection()
    {
        selectionConfirmed = false;
        ClearAllSelections();
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
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isSelected) return true;
        }
        return false;
    }
    
    private void SaveCurrentResult(bool answeredCorrectly, bool applyPenalties = true)
    {
        var result = new QuestionResult(questionId);
        
        // Get selected button indices
        var selectedIndices = new List<int>();
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isSelected) {
                selectedIndices.Add(i);
            }
        }
        result.selectedButtonIndices = selectedIndices.ToArray();
        
        // Get correct button indices
        var correctIndices = new List<int>();
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isCorrectAnswer) {
                correctIndices.Add(i);
            }
        }
        result.correctButtonIndices = correctIndices.ToArray();
        
        // Get button texts (if available)
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].buttonAnimator != null) {
                // Try to get text from child Text component
                var textComponent = buttonConfigs[i].buttonAnimator.GetComponentInChildren<Text>();
                if (textComponent != null) {
                    result.buttonTexts[i] = textComponent.text;
                }
                else {
                    // Try TMPro text component
                    var tmpTextComponent = buttonConfigs[i].buttonAnimator.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if (tmpTextComponent != null) {
                        result.buttonTexts[i] = tmpTextComponent.text;
                    }
                    else {
                        result.buttonTexts[i] = $"Button {i + 1}";
                    }
                }
            }
            else {
                result.buttonTexts[i] = $"Button {i + 1}";
            }
        }
        
        result.wasAnsweredCorrectly = answeredCorrectly;
        result.wasConfirmed = selectionConfirmed;
        
        // Calculate penalties (but only apply if requested)
        int totalFine = 0;
        int totalPoints = 0;
        for (int i = 0; i < buttonConfigs.Length; i++) {
            if (buttonConfigs[i].isSelected && !buttonConfigs[i].isCorrectAnswer) {
                int fine = buttonConfigs[i].fineAmount > 0 ? buttonConfigs[i].fineAmount : defaultFineAmount;
                int points = buttonConfigs[i].penaltyPoints > 0 ? buttonConfigs[i].penaltyPoints : defaultPenaltyPoints;
                totalFine += fine;
                totalPoints += points;
            }
        }
        result.penaltyFineApplied = totalFine;
        result.penaltyPointsApplied = totalPoints;
        
        // Only apply penalties to penaltyTracker if requested
        if (applyPenalties && !answeredCorrectly) {
            if (totalPoints > 0) {
                PenaltyTracker.AddPenalty(totalFine, totalPoints);
            }
            else {
                PenaltyTracker.AddPenalty(totalFine);
            }
            Debug.Log($"[Question {questionId}] Penalty applied: ${totalFine} fine, {totalPoints} points");
        }
        
        QuestionResults.SaveResult(result);
    }
    
    private void RestorePreviousAnswers()
    {
        var savedResult = QuestionResults.GetResult(questionId);
        if (savedResult == null) return;
        
        // Restore button selections
        for (int i = 0; i < savedResult.selectedButtonIndices.Length; i++) {
            int buttonIndex = savedResult.selectedButtonIndices[i];
            if (buttonIndex >= 0 && buttonIndex < buttonConfigs.Length) {
                buttonConfigs[buttonIndex].isSelected = true;
                SetButtonState(buttonIndex, true);
            }
        }
        
        // Restore confirmation state
        if (savedResult.wasConfirmed) {
            selectionConfirmed = true;
            Debug.Log($"[Question {questionId}] Previous answer restored: {(savedResult.wasAnsweredCorrectly ? "Correct" : "Incorrect")}");
        }
    }
    
    // New utility methods
    public void SetQuestionId(int id)
    {
        questionId = id;
    }
    
    public int GetQuestionId()
    {
        return questionId;
    }
    
    public void SetQuestionText(string text)
    {
        questionText = text;
    }
    
    public string GetQuestionText()
    {
        return questionText;
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
    
    private void SetupNavigationButtons()
    {
        // Setup Previous button
        if (previousButton != null) {
            if (string.IsNullOrEmpty(previousSceneName)) {
                previousButton.interactable = false;
            }
            else {
                previousButton.onClick.AddListener(() => SaveAndNavigateToPrevious(previousSceneName));
                previousButton.interactable = true;
            }
        }
        
        // Setup Next button
        if (nextButton != null) {
            if (string.IsNullOrEmpty(nextSceneName)) {
                nextButton.interactable = false;
            }
            else {
                nextButton.onClick.AddListener(() => SaveAndNavigateToNext(nextSceneName));
                nextButton.interactable = true;
            }
        }
    }
    
    // Navigation methods for moving between questions
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
    
    // Helper method to check if a scene exists in build settings
    private bool SceneExistsInBuild(string sceneName)
    {
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            string scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            string sceneNameFromPath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (sceneNameFromPath == sceneName) {
                return true;
            }
        }
        return false;
    }
    
    // Save current selections without confirming the question
    public void SaveCurrentSelections()
    {
        bool answeredCorrectly = CheckIfAnsweredCorrectly();
        SaveCurrentResult(answeredCorrectly, false); // Don't apply penalties during navigation
        Debug.Log($"Saved selections for question {questionId}");
    }
    
    // Final confirmation method for the end of the test
    public static void FinalConfirmAllAnswers(string resultsSceneName = "Results")
    {
        var allResults = QuestionResults.GetAllResults();
        int totalFine = 0;
        int totalPoints = 0;
        
        Debug.Log("[FINAL] Answer check for all questions:");
        
        // Apply penalties for all incorrect answers and log each question
        foreach (var result in allResults) {
            string status = result.wasAnsweredCorrectly ? "✅ Correct" : "❌ Incorrect";
            Debug.Log($"  Question {result.questionId}: {status}");
            
            if (!result.wasAnsweredCorrectly) {
                totalFine += result.penaltyFineApplied;
                totalPoints += result.penaltyPointsApplied;
            }
        }
        
        // Apply total penalties to penalty tracker
        if (totalPoints > 0) {
            PenaltyTracker.AddPenalty(totalFine, totalPoints);
        }
        else if (totalFine > 0) {
            PenaltyTracker.AddPenalty(totalFine);
        }
        
        Debug.Log($"[FINAL] Test completed! Total penalties: ${totalFine} fine, {totalPoints} points");
        
        // Mark all questions as finally confirmed
        QuestionResults.MarkAllAsConfirmed();
        
        // Navigate to results scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(resultsSceneName);
    }
}