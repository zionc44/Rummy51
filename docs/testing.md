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
| 1 | 2,377 | `Tests/Rummy51.Domain.Tests` | the rules | 159 tests |
| 2 | 1,855 | client, `core/` | the twins, and what the screen works out | 125 tests |
| 3 | 2,918 | `Tests/Rummy51.Infrastructure.Tests` | tables, clocks, presence | 89 tests |
| 4 | 1,184 | `Tests/Rummy51.Server.Tests` | the pipeline and the error contract | 31 tests |

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
that the guards are where they say they are, and that 401 comes back rather
than a redirect. 403 was meant to be here too and is not reachable - nothing
authorizes by role, so no route can produce one.

`Rummy51.Application` gets **no suite**. Of its thirty-seven files exactly
one has a method body, and the rest are records and interfaces - a test on
those would be asserting that C# generates a constructor. The one with a
body is `Result<T>`, and it is two static factories: the question worth
asking of it is not whether `Success` is a success but whether it reaches
the client in the right shape, which is the controller's and falls to
sitting 4.

It is exercised all the same, from sitting 3 rather than from the domain -
`Result<T>` lives in `Application`, references point inward, and the domain
suite cannot see it. `Seating` and `Refusals` assert the kind and the code
on every refusal `InMemoryTables` has.

## Where it got to

**1. `Tests/Rummy51.Domain.Tests` - landed.** 159 tests over ten suites
and two fixtures, 1,844 lines in all, against the domain's 2,377. Every
suite lives under `Tests/`, one folder per project held, so the four of
them sit together rather than each beside what it is about. The whole
suite runs in about a tenth of a second, because nothing in it waits for
anything.

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

**2. The client's `core/` - landed.** 125 tests over nine files and 834
lines, beside the 1,855 they hold. They run in about two seconds.

`ng test` had been left as the CLI scaffolded it, pointed at a karma
config whose packages were never installed, so the first thing was to
point it at the vitest that already was. A spec sits beside what it tests,
as Angular looks for it, and `card.spec.ts` is both the suite for `card.ts`
and where every other one gets its `S(4)` and its `joker()` - spelled the
server's way, two digits and zero padded, because a suite that invented
its own spelling would hold the twins to each other and to nothing real.

Held to: what a card is worth, runs and sets and the ace at both ends,
what a meld comes to on the table, the clean run and which one provides
it, the bands a line is lying in, sorting and carrying and where a hand
wraps, where a card let go of would land, the cut read as a ring, and the
client's half of the error contract.

**The twins are checkable now rather than promised.** Rules 5.3 and 5.4
work three examples out in words - six-to-ten with three tens coming to
exactly fifty-one among them - and both suites assert all three, card for
card. Writing them found the first disagreement: `isMeld` on the client
had no minimum length, so two of a suit in order were a meld there and not
on the server. Nothing was reading it that way yet. The server was right
and the client was changed, which is the bargain the twins were written
under.

**3. `Tests/Rummy51.Infrastructure.Tests` - landed.** 89 tests over nine
suites and a fixture, 1,578 lines in all, against the 2,052 of `Tables/`
and `Lobby/` that this sitting is answerable for. They run in about seven
tenths of a second.

`Opened.cs` is the fixture, and everything in it goes through
`InMemoryTables` rather than reaching for the table behind it - who is
asking is half of what this project decides, and a suite that stepped over
that would be holding the other half to nothing. Two things about a table
are drawn at random, the dealer and the seed, so nothing here names a seat
it was not told: a test that wants the player in turn asks the hand who
that is.

The other half of the fixture is `Arranged`, which switches on the testing
aid that deals one seat nothing but melds. A hand that can be gone out with
is one deal in some enormous number, and without it nothing across chapter
9 is reachable at all - no hand ends, so no total is counted, nobody is
knocked out and no game is ever over. It goes out in one strike, which is
rule 6.6, so every hand that ends this way is doubled; nothing here asserts
a figure for that reason, because what a hand is worth is the domain's and
is held there.

`FakeTimeProvider` is what makes the rest of it possible. Rule 3.6 is the
whole of what moves a table nobody is pressing anything at, and every one
of its stages - the cut, the deal, a turn, an offer, the keeping, a
restock, the pause between hands, the question about another game - is a
wait that has to run out. Here one runs out in a line.

Held to: the wait itself and what is left of it; opening and joining and
the one rule that runs through both; chapter 10's two questions about a
seat, which seats are held and which are let go; rule 3.6 acting at every
stage it can; rule 3.5 reached by playing a stock dry, which a person
cannot do by hand; the totals and the knockouts and the places across a
game; rules 9.3 and 10.8, where both of this session's real bugs lived;
what a finished game leaves written down; who is online; and the refusal
codes the client turns into words.

Not held to: the database writer, which wants a real PostgreSQL and is a
project of its own; Identity, which wants a `UserManager`; and the avatar
re-encoding, which wants images.

**Two things it found, neither of them a rule, and both now fixed.** A
throw that emptied the stock left the restock waiting a turn's length
rather than `RestockSeconds` - both throw paths set the turn's wait
without asking what the hand had become, though `WaitForTheHand` next to
them already knew. It was live and it showed: the window saying the pile
is going back into the deck counted down the wrong number.

The second was only latent. `ActForTheOverdue` set the turn wait on the
line after `TheHandEnded`, which would have overwritten the pause between
hands and the question about another game; the human throw beside it did
the two the other way round and was right. It could not bite, for a
reason worth writing down: rule 3.6's auto-play takes back whatever the
turn laid before it throws, so a hand it plays can never be the hand that
ends one, and that call never fired. It would have bitten the day
auto-play could finish a hand, or the day those three waits stopped all
defaulting to fifteen seconds.

One change answers both, and it is a deletion rather than an addition:
both paths ask `WaitForTheHand`, which already maps every stage to its
own length, and both set it before counting the hand, since counting is
what sets the longer wait of the two. The two paths now read the same,
which they had no reason not to. Three tests hold it - the restock, the
pause between hands and the question about another game, each given a
length of its own so that passing means something.


**4. `Tests/Rummy51.Server.Tests` - landed.** 31 tests over three suites
and four fixtures, 608 lines, against the server's 1,212. They run in
about five seconds, which is almost all of it the host booting: these are
the only tests here that start anything.

The pipeline is the real one, booted by `WebApplicationFactory` with
nothing in it replaced - not the cookie scheme, not the exception handler,
not the model binding - because what crosses the wire is the whole of what
this sitting asks about. One thing behind the wire is a double, the
account service, being the only thing a request reaches that wants a
database. `ITables` is left real: it is memory already, and sitting 3 has
just been through it.

The host runs as Testing rather than Development, which matters more than
it reads. In Development the framework puts its own exception page in
front of everything, and what a client is handed instead of an exception
is one of the three things this sitting exists to ask.

Two more things are replaced and neither is a double. The first is the
data protection key ring. The cookie wants a protector and the keys live in a table, so the
pipeline reaches the database by itself whatever the controllers do -
which it cannot here, and spent seconds per run saying so in stack traces
long enough to look like failures. An ephemeral provider ends it.

The second is where the log goes. A run that throws on purpose cannot be
told from a run that broke while both are writing stack traces to the
console, which is not a theory: it happened, the nine the key ring wrote
went unread for a sitting, and the owner reading the output reasonably
concluded the suite was failing when every one of its tests passed. So
nothing reaches the console. What the host writes down is kept, and two
tests hold it - that the handler wrote the exception it was given, which
is the half of the contract that says a traceId leads somewhere, and that
a request which went well wrote nothing down at all. The second of those
is what turns an error nobody asked for from a line somebody has to
notice into a test that fails and names it.

Held to: all three paths of the error contract, asserted out of the body
rather than off the object a controller returned - a `Result` turned into
a problem, model binding refusing a request before any of our code runs,
and something uncaught; that `FailureKind` picks the status that says the
same thing; that `parameters` is always there and in order; that framework
prose becomes `MalformedRequest` rather than arriving as a key the client
would look up; that nothing of an exception crosses; which routes are
behind a credential and which three are the way in, the hub included; that
what comes back is 401 and not the redirect to a login page that cookie
authentication would send by itself; and that an enum crosses as its name
and a player summary carries no secret.

Not held to: anything that needs a signed-in caller. The cookie is real
here, so there is no credential to present without a database behind it,
and the one branch that costs is `/api/auth/me` answering 401 and clearing
the cookie when the account went away under a live session. A test
authentication scheme would reach it and would also replace the thing the
guards suite is here to watch, so it is named rather than covered.

403 is not held either, and cannot be: nothing in the project authorizes
by role, so no route can produce one. The handler that would answer it is
written beside the one for 401 and is the same two lines.

Nor is signing in through Google, which is the third way in. No provider
is configured in this host, so that route answers 500 from inside the
action - and a test reading that as "not 401" would pass for a reason
with nothing to do with what it claims. It was written that way first and
is not there now.

**One thing it found, and it is fixed.** `GlobalExceptionHandler` set the
response content type to `application/problem+json` and then called
`WriteAsJsonAsync`, which set it back to `application/json` - so the line
did nothing, and the uncaught path was the one of the three answering with
a different type from the other two. Nothing was broken by it: the body is
the same shape and the client reads the body. What was wrong is a line
stating an intention the code did not carry out.

It was found the dull way. A test said the third path answers like the
other two, and it did not. The type is now handed to the write rather than
set on the response beside it, and it is said once, in `ApiProblem`, since
two places writing that string are two places that can stop agreeing. All
three paths assert it.


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
