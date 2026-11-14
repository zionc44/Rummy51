# The rules of Rummy 51

The rules as a player would state them. They are written down because they
exist nowhere else: they came out of conversation.

This is where a rule is stated and `Rummy51.Domain` is where it is enforced,
and neither stands without the other. When a rule is decided or changed, it
is changed here in the same breath. Where something below says it is
unsettled, ask rather than decide.

## How to point at a rule

Every rule has a number that belongs to it alone, and code that enforces one
names the number rather than repeating the rule:

```csharp
// rules 5.7, 5.9
public bool AddTo(int seat, int meldAt, int cardId, MeldEnd end)
```

That is the whole of the documentation such a method needs. A rule written
twice is a rule that will be changed once, and the copy that goes stale is
always the one nobody was reading.

**The numbers never move.** A number is given out once and means that rule
for good: a new rule goes at the end of its chapter even where it reads
better in the middle, and a rule that is deleted leaves its number empty
rather than passing it on. Renumbering would quietly repoint every line of
code that names a number, which is the failure this is here to prevent.

## 1. The deck

**1.1** Two packs always, a hundred and four cards, plus the jokers the table
was opened with: none, two or four.

**1.2** The ace is worth eleven wherever it appears, A-2-3 included, which is
why `Rank` starts at two and nothing is worth one. Every picture is worth
ten. Every other card is worth its number.

**1.3** A joker lying on the table is worth whatever the card it stands in
for is worth. A joker in somebody's hand is worth twenty, which is a
different question: on the table it is a card, and in a hand it is a card
nobody got to use.

## 2. Dealing

**2.1** The first dealer is drawn at random and the deal moves on by one
every hand.

**2.2** The player before the dealer cuts, and the card they stop on is the
finishing card.

**2.3** A joker there is the cutter's instead — face up, where the whole
table can see it — and it costs them one of the cards they would have been
dealt, so everyone still ends the deal holding the same number. If the next
card is a joker too, the same again.

**2.4** Then three cards to each player four times round, and a last pass of
three to the first player and two to everyone else: fifteen for the player
after the dealer, fourteen for the rest.

## 3. A turn

**3.1** The player after the dealer opens by discarding. They take no card —
the fifteenth they were dealt is their draw — but like anybody else in their
turn they may put cards on the table before they throw.

**3.2** After that each player in turn takes either the card the player
before them discarded or the top of the stock, and ends by discarding.

**3.3** Only the top of the discard pile is visible.

**3.4** Whatever is taken from the board is back on the board before the turn
ends. A card taken from the discard pile has to go down that same turn, and
so does anything else a player picks up from the table.

**3.5** If the stock runs out, the discard pile is shuffled and becomes the
stock.

**3.6** A turn nobody takes is taken back. Everything that turn did is
undone, last thing first — the runs and sets it laid, and the card it took
off the discard pile, which goes back where it came from — and then a card
off the stock goes straight onto the pile. The player comes back to the hand
they walked away from, one card of the stock the poorer for it, and the table
is as they found it. How long a table waits before deciding a turn is
nobody's is the table's own business and not a rule of the game.

## 4. Runs and sets

**4.1** A run is three or more of the same suit in ascending order; a set is
three or four of the same rank. Neither may hold the same card twice.

**4.2** Q-K-A is a run and A-2-3 is a run. K-A-2 is not, because the ace does
not turn the corner.

## 5. Going down

**5.1** A player needs at least fifty-one points on the table in one move,
and among what they lay there must be a clean ascending run of at least three
cards with no joker in it.

**5.2** The clean three may be part of a longer run that does use a joker:
6-7-8-joker-10 holds one, 5-6-joker-8-9 does not.

**5.3** Everything laid counts for what it is worth, a joker counting as the
card it is standing in for — with one exception, and the exception is the run
that is providing the clean three. Only the clean part of that one counts.

So 6-7-8-joker-10 laid with three tens comes to exactly fifty-one: three tens
are a set and a set can never be anybody's clean run, so the run has to
provide it and is worth twenty-one there rather than thirty-nine. But
5-6-7-joker-9 laid beside a clean run of its own is worth all of thirty-five,
because it is not the one doing the providing. Laid alone it is worth
eighteen, which is the clean three of it.

**5.4** A run that holds a joker and no clean three can never be the one
providing, and so is always worth all of itself: 9-10-joker-Q is thirty-nine
wherever it is laid, and the clean run it is counted beside has to come from
somewhere else.

**5.5** Nothing is ever taken off a set, because a set is not a run and 5.3
is a rule about runs: 7-7-joker is worth twenty-one, the joker counting as
the seven it stands in for.

**5.6** The fifty-one is counted over everything laid in one turn and not
over each run on its own. The turn is the move: cards go down one run at a
time, and what they come to is asked when the turn ends. A player who has
laid something they cannot stand behind cannot throw a card and end the turn
on it — they take it back, or they lay enough to make it good.

**5.7** Only once a player has gone down may they add cards to runs and sets
already on the table, and doing both in the same turn is fine.

**5.8** A player has gone down the moment what they have laid this turn is a
showing, and not when the throw makes it final. That is what lets the two
happen in one turn. It can be lost again in the same turn as well: take back
part of a showing and it stops being one, and until it is good again they may
neither add to the table nor end the turn.

**5.9** A card joins a run at either end of it, where it carries the suit on:
the rank below the lowest card or the rank above the highest. The ace does
not turn the corner here either, so nothing goes above the ace of Q-K-A and
nothing below the ace of A-2-3. A joker joins wherever a card could, and
stands for whatever that place asks for.

**5.10** A card joins a set when it is of that rank and of a suit the set has
not got, and only while the set is three. A set has no ends: four of a rank
are the same four whichever way round they lie.

**5.11** A joker lying in a meld can be bought out of it with the card it is
standing in for, and that card alone: in a run its place names one card
exactly, and in a set it names the rank and leaves the suit to whichever the
set has not got. A set gives its joker up only once it is four, because a
joker in a set is paid for by completing it: two sevens and a joker are
bought with both of the sevens that are missing, the first joining the set
beside the joker and the second taking its place. The joker comes into the
hand that paid for it.

**5.12** Buying a joker is only for a player who has gone down, as 5.7 is,
and a joker bought this way cannot be part of the going down that paid for
it. It goes back onto the table before the turn ends under 3.4, like anything
else taken off it.

## 6. Finishing

**6.1** A player goes out by throwing their last card. Everything else they
held is on the table by then, in runs and sets of their own or added to what
was already lying there: fifteen cards in the hand, fourteen laid down, one
thrown. Whoever does it has won, and the hand ends on that throw.

**6.2** So a hand is never emptied onto the table. Three cards left that
happen to be a run are not a win — laying them would leave nothing to throw,
and a turn ends on a throw and on nothing else. Two of the three are no meld,
so the run waits a turn.

**6.3** Buying a joker is the one way onto the table that cannot empty a
hand: a card goes out and the joker comes in.

**6.4** Going down is allowed in the first round. Finishing is not, until
every other seat has had a turn: a hand that ended sooner would be lost by
players who were never given the chance to go down at all. Whoever completes
the round is not one of them — by their turn everybody else has had theirs —
so the last turn of the first round can take the hand.

**6.5** Nobody is left holding a card they are forbidden to throw. While
going out is closed a hand keeps two cards back from the table instead of
one, so it never reaches the last card it would have to throw.

## 7. The finishing card

**7.1** The card the cut stopped on under 2.2 is what its name says it is. A
player may take it on their turn, in place of the stock and in place of the
pile, and taking it is a promise to end the hand that turn.

**7.2** The promise is a strict one. Everything they put down has to be a run
or a set made out of their own hand: no card of theirs joining what is
already lying on the table, and no joker bought off it. It is the whole hand
at once or it is nothing.

**7.3** So a player who has already gone down may not take the card at all —
they have had their turn on the table.

**7.4** Taking it is a draw like any other in shape, so the hand it joins
goes down to its last card and that card is thrown, and the hand is won under
6.1. A turn that took it and cannot finish ends no other way: it is taken
back, and the card goes back to lying beside the deck. Nothing is lost by
having held it, because the whole table could see it the moment the cut
turned it over.

**7.5** Nobody has to take it, and a hand can be won without it.

## 8. Scoring

**8.1** The winner of a hand takes minus thirty. Everyone else takes the
value of the cards still in their hand — unless they never went down at all,
and then they take a hundred whatever they are holding. A hand nobody ever
showed is not worth counting.

**8.2** Doubling is blanket — it applies to the winner's minus as much as to
everybody else's penalty — and the two conditions stack, so x4 is reachable
when both hold: every player agreeing to a redeal, and a player finishing in
one strike with nothing laid on the board beforehand.

## 9. Hands, games and tables

**9.1** A hand is one deal and a game is a sequence of hands.

**9.2** A table is opened for one of three modes and never changes between
them: a single game, a series to a number of points, and a fixed number of
games with running results.

## 10. Leaving

**10.1** A player who walks away from a game in progress has their hand
played out for them to the end, and the loss is recorded against them.

## 11. What may be seen

**11.1** A player's hand is theirs alone. Nobody else is ever told what is in
it, and nothing that crosses to the table carries a card of it.

**11.2** How many cards each player is holding is public. At a real table
anybody can count them, and it is the one number that says who is close to
going out.

**11.3** Who has gone down is public, their runs and sets being lying there
to be read, and so is everything on the table once it is down.

**11.4** What the player in turn may do next is public: whether they have
taken their card, whether the finishing card is still theirs to take, and who
took the hand when one is taken. The table watches a turn happen.

**11.5** The deck is face down and nothing about it is known - not its order,
and not where the jokers are lying in it. Which pack each card came from is
no secret, a deck of one colour looking only like a printer's mistake.

**11.6** The deal is shown pass by pass: how many cards went where, and never
which.

## 12. A redeal

**12.1** A player may offer the table a redeal, on their turn, after taking
their card and before putting anything on the table. That window is also what
keeps the offer simple: nothing of anybody's is in flight when it is made.

**12.2** Once in a hand. An offer that is refused is not made again by that
player in that hand, though anybody else may still make theirs. An offer that
is taken deals the hand again, and the right comes back with it.

**12.3** Everybody has to agree. One refusal and nothing happens at all: the
turn goes on from where it was left.

**12.4** A player who does not answer in time has agreed. The question stops
the table until it is answered, so silence is somebody who has walked away
from the screen, and that is their own doing. Who agreed and who refused is
shown to a player only once they have answered themselves, so that no answer
is given knowing another.

**12.5** Every card on the table goes back to the player who put it there,
and that includes a joker however many times it has been bought and laid
again since. What a card is worth and which run it was lying in are the
table's; whose it is stays its own from the moment it was laid.

**12.6** Each player then keeps exactly three of their cards and the rest are
shuffled - the whole deck less three for each player. The one who offered the
redeal chooses their three out of fifteen, having taken their card already;
everybody else chooses out of fourteen.

**12.7** The same player cuts again under 2.2, and the deal that follows gives
twelve to the player after the dealer and eleven to everyone else, so that
every hand is the size it was: fifteen and fourteen, three of them old.

**12.8** The deal does not move on under 2.1. It is the same hand, dealt
again.

**12.9** A player dealt four pairs - the same card twice, four times over,
the same rank and the same suit each time - may force a redeal on the table
and does not wait for their turn to do it. The right lasts until they have
finished their own first turn, and it is theirs whether or not they have
already offered one under 12.1.

**12.10** They say which of two it is: put to the table, or the deal ended
there and then.

**12.11** Put to the table, everybody else chooses between the redeal and
the deal ended. Everybody has to choose the redeal; one choosing to end it
ends it, and silence is a redeal as it is under 12.4. Either way the hand is
dealt again - what is being chosen is only what anybody keeps of it, which
is what makes it worth forcing.

**12.12** A deal ended is one nobody keeps anything from. Every card goes
back, and 2.4 deals fifteen and fourteen afresh rather than the twelve and
eleven of 12.7.

**12.13** A turn caught in flight is taken back first, under 3.6. Nothing of
anybody's is in anybody else's hand by the time 12.5 goes looking for who
owns what.

## Unsettled

Questions, not rules, so they have no numbers of their own. A rule written
here before it is settled is a rule somebody will enforce by mistake.

- How a leaver's loss is recorded, which 10.1 leaves open.
