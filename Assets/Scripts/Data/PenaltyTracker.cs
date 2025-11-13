using UnityEngine;

public static class PenaltyTracker
{
    private static int _penaltyFine = 0;
    private static int _penaltyPoints = 0;
    
    
    public static int PenaltyFine => _penaltyFine;
    public static int PenaltyPoints => _penaltyPoints;
    
    public static void AddPenalty(int fineAmount)
    {
        _penaltyFine += fineAmount;
    }
    
    public static void AddPenalty(int fineAmount, int penaltyAmount)
    {
        _penaltyFine += fineAmount;
        _penaltyPoints += penaltyAmount;
    }

    public static void ResetPenalties()
    {
        _penaltyFine = 0;
        _penaltyPoints = 0;
        Debug.Log("PenaltyTracker: All penalties reset");
    }
    
    public static string GetPenaltySummary()
    {
        return $"Fine: {_penaltyFine} DKK, Points: {_penaltyPoints}";
    }
}
