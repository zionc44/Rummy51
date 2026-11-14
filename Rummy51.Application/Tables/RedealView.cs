namespace Rummy51.Application.Tables;

/// <summary>
/// The part of chapter 12 that is one seat's own and not the table's: rule
/// 12.4 masks the answers differently for each of them, and rule 11.1 makes
/// four pairs a secret until they are forced with. No pairs is no right to
/// force, which is why one field says both.
/// </summary>
public sealed record RedealView(IReadOnlyList<bool?> Answers, IReadOnlyList<string> Pairs);
