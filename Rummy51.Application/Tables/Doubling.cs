namespace Rummy51.Application.Tables;

/// <summary>
/// Rule 8.2 as it was arrived at and not only what it came to, so that a
/// minus sixty can say why it is not a minus thirty. Rules 6.6 and 7.1
/// are what can meet its second condition.
/// </summary>
public sealed record Doubling(
    int ForTheRedeal,
    int ForTheWin,
    int Altogether,
    bool Rummy,
    bool FinishingCard);
