# Putting it under test

The decision, the order it happens in, and what each suite is answerable
for. It is written down because it came out of conversation and nothing
else holds it.

## The decision

Every project gets a test suite of its own, one project at a time, one
sitting each. Nothing else is built while a sitting is running, so each
one lands whole or not at all and the list below is also the record of
where it got to.

## What it replaces

Nothing, strictly. What there is today is a harness per change: a console
built against the real code, run once or twice, abandoned in a temporary
folder. Seventeen of them, 2,250 lines, and most no longer compile because
the signatures they call have moved since.

They were worth writing - every one of them answered a question that
reading could not, and two of the bugs they caught would have shipped. But
what a harness proves is that something worked on the day it was written,
which is a different claim from the one that matters.

Three things follow from making the claim standing instead.

**The twins become checkable.** `Melding.cs` beside `melds.ts` and
`Worth.cs` beside `worth.ts` are a duplication the project chose on
purpose, and CLAUDE.md already calls them checkable rather than a promise.
They are not. Nothing compares them, and the only reason they agree is
that whoever last changed one remembered the other. Two suites fed the
same hands, asserting the same answers, is what makes that sentence true -
and it is the one gain here that pays off a debt rather than adding cover.

**Rules nobody can reach by hand get reached.** Four pairs is one deal in
about a hundred and twenty; a joker at the cut turns up twice in a hundred hands; a hand
that can actually be gone out with is rarer than either. Today they are
reached by switching on something in `Testing` and playing until they turn
up, and then by remembering to switch it off again. A test reaches them by
construction, in milliseconds, by trying seeds until one appears.

**A rule change gets an answer to "what else did that break".** There is
no answer today. With a suite named by rule number, the failing tests are
the answer and they name the rules.

## The order

Measured, and ordered by what is at risk rather than by size.

| | lines | suite | sitting | stands at |
|---|---|---|---|---|
| 1 | 2,377 | `Tests/Rummy51.Domain.Tests` | the rules | 136 tests |
| 2 | 1,753 | client, `core/` | the twins, and what the screen works out | |
| 3 | 2,918 | `Tests/Rummy51.Infrastructure.Tests` | tables, clocks, presence | |
| 4 | 1,184 | `Tests/Rummy51.Server.Tests` | the pipeline and the error contract | |

**Domain first** because it is the product. It has no dependencies at all -
no database, no clock, no HTTP - so a suite against it needs no scaffolding
beyond getting a `Hand` into a given state, and that fixture is most of the
work. Target the rules that have already bitten before reaching for cover:
8.2 and the doubling, the whole of chapter 12, and 5.1 against going down.

**The client second**, although it is smaller and less central, because
`vitest` is already a dependency and `ng test` is already wired, and
because this is the sitting that makes the twins real. Nothing else here
has that property.

**Infrastructure third.** `OpenTable` is where this session's two real
bugs lived - a timestamp that refreshed when it should have been taken
once, and a question asked of the wrong array - and both were about
presence and clocks over time, which is exactly what `FakeTimeProvider`
exists for. Higher risk than the two above it: the state is real and the
orderings are many. The database writer is **not** in this sitting; that
wants a real PostgreSQL or Testcontainers and is a project of its own.

**The server last**, as integration tests through `WebApplicationFactory`:
that the three paths of the error contract all still produce the one shape,
that the guards are where they say they are, that 401 and 403 come back
rather than a redirect.

`Rummy51.Application` gets **no suite**. One file in it has a method body;
the rest is contracts and views. `Result<T>` is covered from the domain
sitting, where it is actually exercised.

## Where it got to

**1. `Tests/Rummy51.Domain.Tests` - landed.** 136 tests over eleven files
and 1,605 lines, against the domain's 2,377. Every suite lives under
`Tests/`, one folder per project held, so the four of them sit together
rather than each beside what it is about. The whole suite runs in about a
tenth of a second, because nothing in it waits for anything.

`Dealt.cs` is the fixture, and the fixture was most of the sitting.
`Holding(cards)` lays a whole deck out so that the opener is dealt exactly
the cards asked for - which is what reaches four pairs, a joker at the cut
and a hand that can be gone out with by construction, rather than by trying
seeds until one turns up. It finds where each card has to lie by calling
`Dealing.Deal` and reading the passes it reports back, instead of working
the positions out a second time: a fixture that duplicated the dealing
arithmetic would agree with itself and with nothing else.

Held to: the deck and what a card is worth, runs and sets and the joker in
one, going down and the fifty-one, joining and buying, the turn and its
undo, the finishing card and what closes it, the score and the places, both
kinds of redeal, and the seats that sit out of a survival hand.

Not held to: anything that needs a clock, a table or a wire. Those are
sittings 3 and 4.

## How a test is named

By the rule it holds the code to, and by nothing else:

```csharp
[Fact]
public void Rule_12_2_gives_the_right_back_with_every_deal()
```

So `grep` on a number finds the rule, the code that enforces it, and the
tests that hold it there - which is the same bargain the code already
makes. It also leaves `rule-links.js` a third thing it could one day
count, turning it from a map of what is enforced into a map of what is
covered.

It already tells the two apart. A pointer from under `Tests/` is counted
on its own, and a rule held to by a test and by nothing that ships gets a
list of its own - because a rule like that is not enforced, however well it
is checked. None is, today.

## What none of this will do

It will not find what nobody thought of. The bugs caught this session were
caught by thinking; a suite keeps a fix fixed, it does not replace the
thinking that found it.

It will not test the screen. The melds spilling under the buttons, the
drag, the animations, how the board fits - no suite described here would
have caught any of it, and the first two of those are where the bugs
actually are.

And it costs. From the first sitting onward, a change in behaviour is also
a change in tests, and a file with a hundred tests against it is a file
that is harder to split than `Hand.Redeal.cs` was.
