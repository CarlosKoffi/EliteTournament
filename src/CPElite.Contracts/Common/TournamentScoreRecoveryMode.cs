namespace CPElite.Contracts.Common;

public enum TournamentScoreRecoveryMode
{
    ManualOnly = 1,
    AutomaticEveryInterval = 2,
    AfterEachMatch = 3,
    EndOfRound = 4,
    EndOfTournament = 5
}
