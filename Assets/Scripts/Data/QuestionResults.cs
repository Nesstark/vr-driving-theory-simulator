using System.Collections.Generic;
using UnityEngine;

public static class QuestionResults
{
    private static Dictionary<int, QuestionResult> savedResults = new Dictionary<int, QuestionResult>();

    public static void SaveResult(QuestionResult result)
    {
        savedResults[result.questionId] = result;
        Debug.Log($"Saved result for question {result.questionId}");
    }

    public static QuestionResult GetResult(int questionId)
    {
        savedResults.TryGetValue(questionId, out QuestionResult result);
        return result;
    }

    public static bool HasResult(int questionId)
    {
        return savedResults.ContainsKey(questionId);
    }

    public static QuestionResult[] GetAllResults()
    {
        var results = new QuestionResult[savedResults.Count];
        savedResults.Values.CopyTo(results, 0);
        return results;
    }

    public static void ClearAllResults()
    {
        savedResults.Clear();
    }

    public static int GetTotalQuestionsAnswered()
    {
        int count = 0;
        foreach (var result in savedResults.Values)
        {
            if (result.wasConfirmed) count++;
        }
        return count;
    }

    public static int GetTotalCorrectAnswers()
    {
        int count = 0;
        foreach (var result in savedResults.Values)
        {
            if (result.wasConfirmed && result.wasAnsweredCorrectly) count++;
        }
        return count;
    }
    
    public static void MarkAllAsConfirmed()
    {
        foreach (var result in savedResults.Values)
        {
            result.wasConfirmed = true;
        }
        Debug.Log("Marked all questions as confirmed.");
    }
}