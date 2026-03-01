namespace AppLogica.Desk.Domain.Problems;

/// <summary>
/// ITIL-aligned problem lifecycle states.
/// </summary>
public enum ProblemStatus
{
    New = 0,
    Open = 1,
    Investigating = 2,
    RootCauseIdentified = 3,
    KnownError = 4,
    Resolved = 5,
    Closed = 6
}
