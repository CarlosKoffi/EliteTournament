namespace CPElite.Contracts.Common;

public enum ScoreReconciliationStatus
{
    PerfectMatch = 1,
    NeedsReview = 2,
    Dispute = 3,
    NoCandidateFound = 4
}
