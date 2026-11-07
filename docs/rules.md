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
`Rank` starts at two and nothing is worth one. Every picture is worth ten. A
joker is worth whatever the card it stands in for is worth.

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
6-7-8-joker-10 holds one, 5-6-joker-8-9 does not. Only the clean part of
such a run counts towards the fifty-one, so 6-7-8-joker-10 is worth
twenty-one there and not forty — which is why 6-7-8-joker-10 laid down
with three tens comes to exactly fifty-one and is allowed by a hair.

Only once a player has gone down may they add cards to runs and sets already
on the table, and doing both in the same turn is fine.

Going down is allowed in the first round; finishing is not.

## Scoring

The winner of a hand takes minus thirty; everyone else takes the value of the
cards still in their hand.

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
