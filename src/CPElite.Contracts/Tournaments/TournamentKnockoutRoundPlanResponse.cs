using CPElite.Contracts.Common;

namespace CPElite.Contracts.Tournaments;

public sealed record TournamentKnockoutRoundPlanResponse(TournamentStage Stage, int MatchCount, string Label);
