namespace Rummy51.Application.Tables;

/// <summary>
/// Rules 8.2 and 8.4 as they were arrived at and not only what they came
/// to, so that a minus sixty can say why it is not a minus thirty. Rules
/// 6.6 and 7.1 are what can meet 8.2's second condition.
/// </summary>
public sealed record Doubling(
    int ForTheSeries,
    int ForTheRedeal,
    int ForTheWin,
    int Altogether,
    bool Rummy,
    bool FinishingCard);
