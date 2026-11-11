# The rules of Rummy 51

The rules as a player would state them. They are written down because they
exist nowhere else: they came out of conversation, and most of the code that
enforces them is not written yet.

This is the reference, not the authority — the authority is
`Rummy51.Domain`, and a rule is only really settled once it is enforced
there. Until then this is all there is, so keep it right: when a rule is
decided or changed, it is changed here in the same breath. Where something
below says it is unsettled, ask rather than decide.

## The deck

Two packs always, a hundred and four cards, plus the jokers the table was
opened with: none, two or four.

The ace is worth eleven wherever it appears, A-2-3 included, which is why
`Rank` starts at two and nothing is worth one. Every picture is worth ten.
Every other card is worth its number.

A joker lying on the table is worth whatever the card it stands in for is
worth. A joker in somebody's hand is worth twenty, which is a different
question: on the table it is a card, and in a hand it is a card nobody got
to use.

## Dealing

The first dealer is drawn at random and the deal moves on by one every hand.

The player before the dealer cuts, and the card they stop on is the finishing
card. A joker there is theirs instead — face up, where the whole table can
see it — and it costs them one of the cards they would have been dealt, so
everyone still ends the deal holding the same number. If the next card is a
joker too, the same again.

Then three cards to each player four times round, and a last pass of three to
the first player and two to everyone else: fifteen for the player after the
dealer, fourteen for the rest.

## A turn

The player after the dealer opens by discarding and doing nothing else.

After that each player in turn takes either the card the player before them
discarded or the top of the stock, and ends by discarding. Only the top of
the discard pile is visible.

Whatever is taken from the board is back on the board before the turn ends.
A card taken from the discard pile has to go down that same turn, and so
does anything else a player picks up from the table.

If the stock runs out, the discard pile is shuffled and becomes the stock.

A turn nobody takes is taken back. Everything that turn did is undone, last
thing first - the runs and sets it laid, and the card it took off the discard
pile, which goes back where it came from - and then a card off the stock goes
straight onto the pile. The player comes back to the hand they walked away
from, one card of the stock the poorer for it, and the table is as they found
it. How long a table waits before deciding a turn is nobody's is the table's
own business and not a rule of the game.

## Runs and sets

A run is three or more of the same suit in ascending order; a set is three or
four of the same rank. Neither may hold the same card twice.

Q-K-A is a run and A-2-3 is a run. K-A-2 is not, because the ace does not
turn the corner.

## Going down

A player needs at least fifty-one points on the table in one move, and among
what they lay there must be a clean ascending run of at least three cards
with no joker in it.

The clean three may be part of a longer run that does use a joker:
6-7-8-joker-10 holds one, 5-6-joker-8-9 does not.

Everything laid counts for what it is worth, a joker counting as the card it
is standing in for — with one exception, and the exception is the run that is
providing the clean three. Only the clean part of that one counts.

So 6-7-8-joker-10 laid with three tens comes to exactly fifty-one: three tens
are a set and a set can never be anybody's clean run, so the run has to
provide it and is worth twenty-one there rather than thirty-nine. But
5-6-7-joker-9 laid beside a clean run of its own is worth all of thirty-five,
because it is not the one doing the providing. Laid alone it is worth
eighteen, which is the clean three of it.

A run that holds a joker and no clean three can never be the one providing,
and so is always worth all of itself: 9-10-joker-Q is thirty-nine wherever it
is laid, and the clean run it is counted beside has to come from somewhere
else.

Nothing is ever taken off a set, because a set is not a run and this is a
rule about runs: 7-7-joker is worth twenty-one, the joker counting as the
seven it stands in for.

The fifty-one is counted over everything laid in one turn and not over each
run on its own. The turn is the move: cards go down one run at a time, and
what they come to is asked when the turn ends. A player who has laid
something they cannot stand behind cannot throw a card and end the turn on
it — they take it back, or they lay enough to make it good.

Only once a player has gone down may they add cards to runs and sets already
on the table, and doing both in the same turn is fine.

A player has gone down the moment what they have laid this turn is a showing,
and not when the throw makes it final. That is what lets the two happen in
one turn. It can be lost again in the same turn as well: take back part of a
showing and it stops being one, and until it is good again they may neither
add to the table nor end the turn.

A card joins a run at either end of it, where it carries the suit on: the
rank below the lowest card or the rank above the highest. The ace does not
turn the corner here either, so nothing goes above the ace of Q-K-A and
nothing below the ace of A-2-3. A joker joins wherever a card could, and
stands for whatever that place asks for.

A card joins a set when it is of that rank and of a suit the set has not
got, and only while the set is three. A set has no ends: four of a rank are
the same four whichever way round they lie.

A joker lying in a meld can be bought out of it with the card it is standing
in for, and that card alone: in a run its place names one card exactly, and
in a set it names the rank and leaves the suit to whichever the set has not
got. A set gives its joker up only once it is four, because a joker in a set
is paid for by completing it: two sevens and a joker are bought with both of
the sevens that are missing, the first joining the set beside the joker and
the second taking its place. The joker comes into the hand that paid for it.
Like adding to a meld, this is only for a player who has gone down - and a
joker bought this way cannot be part of the going down that paid for it, nor
stay in the hand past the end of the turn, because it was taken from the
table and everything taken from the table is back on it before the turn
ends.

Going down is allowed in the first round; finishing is not.

## Scoring

The winner of a hand takes minus thirty. Everyone else takes the value of the
cards still in their hand — unless they never went down at all, and then they
take a hundred whatever they are holding. A hand nobody ever showed is not
worth counting.

Doubling is blanket — it applies to the winner's minus as much as to
everybody else's penalty — and the two conditions stack, so x4 is reachable
when both hold: every player agreeing to a redeal, and a player finishing in
one strike with nothing laid on the board beforehand.

## Hands, games and tables

A hand is one deal and a game is a sequence of hands.

A table is opened for one of three modes and never changes between them: a
single game, a series to a number of points, and a fixed number of games with
running results.

## Leaving

A player who walks away from a game in progress has their hand played out for
them to the end, and the loss is recorded against them. How it is recorded is
unsettled.

## Unsettled

- What the finishing card actually does.
- How a leaver's loss is recorded.
