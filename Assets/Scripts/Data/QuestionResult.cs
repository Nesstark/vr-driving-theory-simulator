using System;

[System.Serializable]
public class QuestionResult
{
    public int questionId;
    public int[] selectedButtonIndices;
    public int[] correctButtonIndices;
    public string[] buttonTexts;
    public bool wasAnsweredCorrectly;
    public bool wasConfirmed;
    public int penaltyFineApplied;
    public int penaltyPointsApplied;
    public DateTime timestamp;
    
    public QuestionResult(int id)
    {
        questionId = id;
        selectedButtonIndices = new int[0];
        correctButtonIndices = new int[0];
        buttonTexts = new string[4];
        wasAnsweredCorrectly = false;
        wasConfirmed = false;
        penaltyFineApplied = 0;
        penaltyPointsApplied = 0;
        timestamp = DateTime.Now;
    }
}