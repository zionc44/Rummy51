namespace Rummy51.Domain.Game;

/// <summary>
/// A redeal put to the table, and what the seats have said about it.
/// Chapter 12.
///
/// Offering is agreeing, so the seat that made it is answered from the
/// start. That is also why it is the one answer anybody may see before
/// they have given their own: it was given before the question existed.
/// </summary>
internal sealed class Offer
{
    private readonly bool?[] answers;
    private readonly bool[] read;

    public Offer(int by, int seats)
    {
        By = by;
        answers = new bool?[seats];
        read = new bool[seats];
        answers[by] = true;
    }

    public int By { get; }

    public IReadOnlyList<bool?> Answers => answers;

    /// <summary>Somebody has still to say, so the question is open.</summary>
    public bool Open => answers.Any(answer => answer is null);

    /// <summary>Rule 12.3.</summary>
    public bool Agreed => answers.All(answer => answer == true);

    public bool EverybodyHasRead => read.All(one => one);

    public bool Answer(int seat, bool agreed)
    {
        if (!Holds(seat) || answers[seat] is not null)
        {
            return false;
        }

        answers[seat] = agreed;

        return true;
    }

    /// <summary>Rule 12.4.</summary>
    public void TakeSilenceAsAgreement()
    {
        for (var seat = 0; seat < answers.Length; seat++)
        {
            answers[seat] ??= true;
        }
    }

    public bool Read(int seat)
    {
        if (!Holds(seat) || read[seat])
        {
            return false;
        }

        read[seat] = true;

        return true;
    }

    /// <summary>
    /// What this seat may be shown. Rule 12.4 keeps every answer but the
    /// offerer's from somebody who has not answered, so that no answer is
    /// given knowing another.
    /// </summary>
    public IReadOnlyList<bool?> AsSeenBy(int seat) =>
        Holds(seat) && answers[seat] is null
            ? [.. answers.Select((answer, at) => at == By ? answer : null)]
            : answers;

    private bool Holds(int seat) => seat >= 0 && seat < answers.Length;
}
