using UnityEngine;

public static class penaltyTracker
{
    private static int _penaltyFine = 0;
    private static int _penaltyPoints = 0;
    private static bool _isSuspended = false;
    private static readonly int suspensionThreshold = 3;
    
    
    public static int PenaltyFine => _penaltyFine;
    public static int PenaltyPoints => _penaltyPoints;
    public static bool IsSuspended => _isSuspended;
    
    public static void AddPenalty(int fineAmount)
    {
        _penaltyFine += fineAmount;
        CheckSuspensionStatus();
    }
    
    public static void AddPenalty(int fineAmount, int penaltyAmount)
    {
        _penaltyFine += fineAmount;
        _penaltyPoints += penaltyAmount;
        CheckSuspensionStatus();
    }
    
    private static void CheckSuspensionStatus()
    {
        if (_penaltyPoints >= suspensionThreshold) {
            _isSuspended = true;
        }
    }

    private static void ResetPenalties()
    {
        _penaltyFine = 0;
        _penaltyPoints = 0;
        _isSuspended = false;
    }
    
    public static string GetPenaltySummary()
    {
        return $"Fine: ${_penaltyFine}, Points: {_penaltyPoints}, Suspended: {_isSuspended}";
    }
}
