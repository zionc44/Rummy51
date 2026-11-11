# Rummy51

An Israeli-popular variant of Rummy (not an Israeli game — Rummikub is the
Israeli one, and it is different): ASP.NET Core API + Angular client,
multiplayer only.

## Working agreements

- **Never commit without being asked.** Finish, report, stop.
- One step at a time. Explain a design before writing it.
- Code, comments and commits in English. Conversation in Hebrew.
- Hold the code to the rules below, and say so when a change would break one.

## How the code should read

Guides, not gates — a rule may be broken when following it would hurt, and that
is worth a word in the commit message.

Nothing enforces any of this, which is how a seat component came to hold a drag
engine and a page component a whole animation director: no commit added them,
each one grew a reasonable piece at a time and nobody saw the total. So one
alarm counts what the reading cannot. `.claude/hooks/file-size.js` says when a
hand-written file has passed the size at which everything healthy here sits —
250 lines of TypeScript or C#, 180 of a template — and asks whether it is still
one job. It blocks nothing and the number is not a budget to write up to; it
only makes somebody look. Its silence is not a clean bill of health, and the
answer to it is sometimes no: `dealing.ts` sits above the line on purpose.

```bash
node .claude/hooks/file-size.js --all      # every file, not just the one just written
```

That second half matters more than it sounds. As a hook it only ever sees what
is being touched, so a file that grew and was then left alone is never asked
about again — and if the hook misses a run, which has happened, nothing says
so. The sweep is what makes a deliberate look cheap: run it when a feature
lands, read the two or three files it names, and decide. It says where; only a
reader says whether.

**Functions**

- One thing, and the name says what, not how. If the name needs "and", it is
  two functions.
- Readable without scrolling. If it does not fit, the shape is wrong, not the
  screen. No line count: a budget only buys functions cut mid-thought.
- One level of abstraction. Orchestrating three calls and then parsing a string
  is two jobs.
- Guard clauses over nesting. A condition that ends the work returns at once.

**Components**

- A component either arranges other components or renders one thing.
- Logic that is not presentation belongs in a service. The component asks and
  shows; it does not decide.
- A template that needs a comment to follow is two components.

**Reuse**

- Extract on the third occurrence, not the second. Resemblance between two
  places is usually coincidence, and merging them too early is what grows the
  flag parameters nobody dares touch. Duplication is cheap; the wrong
  abstraction is not.
- Share behaviour, not shape.
- A parameter added only to make something reusable means there was no reuse.

**Everywhere**

- Names instead of comments. Comment *why*, never *what*.
- Delete rather than comment out. Git remembers.
- Read the whole file before giving something a second job. A targeted edit
  shows a diff, never a size, and a file is only ever read by whoever opens it.

## Layout

```
Rummy51.Domain          entities, game rules       no project references
Rummy51.Application     use cases, contracts       -> Domain
Rummy51.Infrastructure  EF Core, Identity, impls   -> Application
Rummy51.Server          controllers, DI, pipeline  -> Application, Infrastructure
rummy51.client          Angular 22
```

References point inward. `Application` declares an interface; `Infrastructure`
implements it and registers it in `AddInfrastructure`. Anything web-specific
(the auth cookie, the HTTP pipeline) belongs to the server, not Infrastructure.

## Running it

The halves start separately — do not re-add `ASPNETCORE_HOSTINGSTARTUPASSEMBLIES`
to `launchSettings.json`.

- Server: F5 in Visual Studio (`https` profile), opens `/swagger`
- Client: `cd rummy51.client && npm start` → `https://localhost:57541`

`npm run lint` is the client's other guard: six rules of our own in
`eslint.config.js`, each answering to a line written above, plus what Angular
and TypeScript recommend. The list is short on purpose — a hundred rules
switched on and half switched off again is a file nobody understands. The
server has no equivalent; `Nullable` is on, which is the large one, and no
analyzer settings beyond that.

The dev server proxies `/api` to `https://localhost:7115` (`src/proxy.conf.js`,
override with `API_TARGET`), so no CORS in development. `SpaRoot` and the
`.esproj` reference stay in the csproj: publish bundles Angular into `wwwroot`.

To look at it on a phone on the same network, `npm run lan` serves on every
interface rather than on loopback alone. Allow TCP 57541 through the firewall
on the private profile once, then open `https://<this machine's address>:57541`
there. Only that one port is exposed: the dev server reaches the API over
loopback as it always does. The ASP.NET development certificate names
`localhost` and not an address, so the phone warns and the warning has to be
accepted; everything behind it works, the hub included. Signing in through
Google does not, because that address is not a registered redirect - use an
email and a password.

A build failing with MSB3027 means the server is running and holding its output.
Stop it, or build with `-p:BaseOutputPath=<temp>/` to check compilation only.

`Testing` in `appsettings.Development.json` holds aids for testing a game by
hand. The section is bound only when the host is running in development, so
writing it into a deployed configuration switches nothing on. `MarkJokers`
tells every client where the jokers are lying in the deck, which is the only
practical way to work on a cut that finds one twice in a hundred hands — and
plainly a cheat once that deck becomes the stock.

## Database

PostgreSQL. Connection string in `appsettings.Development.json` (local machine,
by the owner's decision).

`EFCore.NamingConventions` maps identifiers to `snake_case`. Identity's tables
are renamed in `Rummy51DbContext.OnModelCreating`:

```
players  roles  player_claims  player_logins  player_tokens  player_roles  role_claims
```

`__EFMigrationsHistory` keeps its default name — renaming it after a migration
is recorded leaves EF unable to find its own bookkeeping.

```bash
dotnet ef migrations add <Name> --project Rummy51.Infrastructure --startup-project Rummy51.Server --output-dir Persistence/Migrations
dotnet ef database update --project Rummy51.Infrastructure --startup-project Rummy51.Server
```

## Identity and authentication

`Player : IdentityUser<Guid>` lives in `Domain`; the email is also the
`UserName`, unique in the database as well as in `RequireUniqueEmail`.
`DisplayName` is separate: free text, may be Hebrew, need not be unique.

**Cookies, not JWT** — Angular is served from the server's own `wwwroot`, so the
browser can carry an `httpOnly`, encrypted, sliding credential. `SameSite=Lax`.
Cookie auth's redirect-to-login default is replaced with 401/403 in
`CookieAuthenticationSetup`, since this is an API.

The cookie is opaque to the client, so `GET /api/auth/me` is how Angular learns
who is signed in.

- Timings from the `Authentication` config section, never from code.
- Never handle a password outside `UserManager`.
- `EmailConfirmed` is set on registration **because nothing sends mail yet**.
  So is the absence of password reset — a typo in the password strands an
  account. Both change once there is a mail sender.
- Role tables exist, unused. `PhoneNumber` inherited, unused.
- `InvalidCredentials` covers both an unknown address and a wrong password:
  distinguishing them reveals which addresses are registered.

## Error contract

**The server never sends display text.** Every failure is a stable code plus an
ordered parameter array; the client owns the wording.

```json
{ "status": 400, "title": "ValidationFailed", "traceId": "...",
  "errors": [{ "code": "PasswordTooShort", "parameters": ["8"] }] }
```

`parameters` is always present, possibly empty. `title` and `type` are for
whoever reads the response by hand.

Three paths produce that shape and all three must keep producing it:
`Result<T>` → `ApiProblem` in a controller, `ModelStateErrors` for model
binding, and `GlobalExceptionHandler` for anything uncaught.

- **Expected failures are returned, not thrown.** `Result<T>` carries
  `FailureKind` so the controller picks 400, 401 or 409.
- Codes name field and rule together (`EmailRequired`) so most need no
  parameters.
- A code is letters and digits only; framework prose becomes
  `MalformedRequest` rather than reaching the client as a lookup key.
- Request DTOs avoid the `required` modifier: `System.Text.Json` would reject a
  missing property in English prose before validation runs.
- An exception never reaches the client. `traceId` links it to the log.

## Client

Angular 22, standalone, **zoneless** — `zone.js` is not installed. Template
state must be a `signal`, or an async assignment will never render. Use `@if` /
`@for`.

**Three files per component, always**: `.ts`, `.html`, `.scss`. No inline
template, no inline styles, even where the stylesheet is nearly empty.

**One component to a folder**, and folder, files, class and selector all say
the same name: `field-error/field-error.ts` holds `FieldError`, selected as
`app-field-error`. A wrapper small enough to feel like it belongs beside its
sibling still gets its own folder. The one exception is the root, `app.ts`
sitting directly in `src/app` as `app-root`, which is Angular's own layout.

SCSS, no utility framework. Tokens are custom properties in
`styles/_tokens.scss`, so a stylesheet uses `var(--felt-500)` without importing
anything. `styles.scss` holds the reset and the few element styles that several
pages share — `.btn-primary`, `.btn-ghost`, `.field-input`. Breakpoints come
from the `from()` mixin: `@use 'breakpoints' as *;`.

**Logical properties only** — `margin-inline-start`, `inset-inline-end`,
`padding-block`; never `left`, `right`, `margin-left`. That is what makes the
layout mirror when the language changes, and a stray `margin-left` is only
noticed by whoever reads English. Exception: a wrapper around an email or
password input is `dir="ltr"`, since both read left to right in any language.

**An icon is a component**, not markup in a template and not an `<img>`. An
image file cannot be recoloured — `currentColor` does not reach inside it — and
these icons take their colour from whatever contains them. A picture whose
colours are fixed, such as a logo, is a file and an `<img>`.

A card is one of those: an `<img>` of an SVG in `public/cards`. There are two
decks. The second, `cards/corners`, is the first with both indices drawn
twice the size and with a heavier pen, and the pips and pictures between them
shrunk to stay out of their way — which is what makes a card legible when all
that shows of it is its corner. Weight carries at that size where height does
not: a rank is a stroked path, so it is one number, and eighty is the widest
pen that fits the box the glyph is drawn in, which is why the box is also told
to stop clipping. Everything on screen reads from it, through
`pictureOf` in `core/cards/deck.ts`, which is the one place the path is
written. It was meant for the hand alone and the whole-card deck for
everywhere a card is seen whole - but cards lie over one another nearly
everywhere they are shown here, and where they do not, the larger index
costs nothing. Nothing reads `public/cards` any more; it is the deck this
one is generated from. It is generated and never drawn by hand — `npm run cards` builds it from the source deck, so a change there can
be carried over.

ngx-translate loads `public/i18n/<code>.json` at runtime. Hebrew is the default
and carries `dir="rtl"`. Adding a language: an entry in `LANGUAGES`, a widening
of `LanguageCode`, and the JSON file.

Server error codes are translation keys under `errors.`; the ordered parameters
become `{{0}}`, `{{1}}`. Forms validate the same rules as the server and express
failures as the same codes, so one table and one component serve both.

A session is restored before the first render (`provideAppInitializer`), and
guards run both ways — signed out away from `/lobby`, signed in away from the
forms.

## The game

The rules live in [`docs/rules.md`](docs/rules.md) — the deck, the deal, a
turn, runs and sets, going down, scoring, and what is still unsettled. Read
it before touching anything under `Rummy51.Domain/Game`, and settle a rule
there in the same change that enforces it, or the two drift apart and the
written one is the one everybody trusts.

## State of play

Done: project layout, player table, error contract, Swagger at `/swagger`,
`/api/auth` (`register`, `login`, `logout`, `me`), signing in through Google
and linking it to an account that already holds the address, the lobby roster
over SignalR, and `/api/account` behind a profile menu in the header — display
name, and a password that can be added as well as changed.

A picture can be uploaded, cropped in the browser and re-encoded on the
server, which keeps nothing of the original file — its EXIF, and the GPS a
phone writes into it, included. It lives in `player_avatars` rather than on
the player row, which Identity reads on every authenticated request, and is
served from a URL carrying a tag of its contents so the answer can be cached
for good. A provider's picture and the player's own are both kept; the player
says which to show.

A table is opened for a number of seats, a mode and a number of jokers, and
starts the moment the last seat is taken. One rule runs through all of it: a
player occupies at most one seat, so opening a table and joining one are the
same move and neither is possible while already seated. Tables live in memory
(`InMemoryTables`) and nothing about them is in the database. A hand reaches
the players at that table over a SignalR group rather than the lobby, which
was arranged before any cards existed because of what it carries next.

The deck is two packs and the jokers the table was opened with. A card is one
of the fifty-two or a joker and nothing else; the ace is fourteen, because it
is worth eleven wherever it is used, A-2-3 included. Shuffling is Fisher-Yates
from a seed that is kept, so an odd hand can be dealt again exactly as it was.
The cutter clicks a card in the fan laid out on the felt: that card is the
finishing card, and a joker there is theirs instead — it travels to their seat
and lies face up for the table to read — and costs them one of the cards they
would have been dealt, so everyone still ends the deal holding the same
number. The deal runs from there, and it is where secrecy starts: a hand
reaches the player holding it and nobody else, while every other seat is told
a count and shows a pile of backs.

A hand is held overlapped and reads left to right, lowest on the left. That
line and the melds laid on the table are the only places in the client that
do not mirror with the language, because a row of cards is not writing: the
index a card is known by is printed in its top left corner, so a card has to
be covered on its right and not on its left. All that shows of a card is the
corner that names it, and fifteen cards in one line across a phone leave
seventeen pixels of each — not a card anybody can read — so below the `sm`
breakpoint the hand is laid in two lines and the corner doubles. A line keeps
a place for every card it will hold from the moment the first one lands, so
nothing shifts as the deal goes on, and the felt gives up the room for both
lines before the cut rather than in the middle of it.

The hand is the player's to arrange. One press of the sort button lays it by
rank and the next by suit, because going down needs a run and a set at once
and no single order shows both; jokers go past the highest card and the ace
sorts beside the king, which is the one thing no single line can help. A card
is dragged where it is wanted and the rest part to let it in, and where the
hand wraps is the player's too — only carrying a card between the lines moves
the break, so a run gathered on one line stays there. None of this crosses
the wire: the server sends a hand and is never told an order.

What a run and a set are is written twice, on purpose. `Melding.cs` is the
one that decides - the server refuses anything it disagrees with - and
`core/cards/melds.ts` is there so the screen can answer the player without
asking. The copy exists because the thing the screen reads is the hand as
its owner has arranged it, and an arrangement never crosses the wire: a band
that waited for a round trip would lag the finger that drew it. The same
goes for `Worth.cs` and `core/cards/worth.ts`. Each pair is changed together
or not at all, and where they disagree the server is right.

Runs and sets are banded as they are laid. The scan reads the hand as it is
arranged rather than searching it for the best it could be — a meld is a meld
because the player has put the cards beside each other, which is also why one
broken across two lines is not one. Being a meld and being allowed to go down
are different questions, and both are now asked. The band is deep enough to take hold of, and that is
what it is for: a finger on the band carries the whole run, a finger on the
card above it carries one card out of it.

A turn is a card in and a card out, and between them cards onto the table.
Tapping the band that marks a run lays that run down - the band already says
these cards go together, so dragging it carries them together and tapping it
plays them together. Several may be laid in one turn and one card still has
to be thrown afterwards.

Going down is three questions and they are asked in three places, because
they fall due at three different moments. A meld is refused as it is laid if
it is not a run or a set. What a turn has laid has to come to fifty-one with
a clean run among it, and that is asked when the throw ends the turn, since
until the throw lands anything can still be taken back. And a player has
gone down the moment what they laid stands as a showing rather than when the
throw makes it final - which is what lets going down and adding to the table
happen in one turn, and what takes the right away again if part of the
showing is taken back.

Only a player who is down may add a card to a meld lying there, or buy a
joker out of one with the card it stands for. And whatever is taken off the
table is back on it before the turn ends - the card off the discard pile and
a bought joker are one rule and not two, so there is one account of what a
turn owes and one question asked of the hand. A card off the stock owes
nothing; the stock is not the table. A throw refused says which of the two
caught the player, and the card that has to go back takes the place of the
one they tried to throw, so it is standing clear when the notice is read and
the next place they press lays it.

The screen says where a hand stands before any of that is asked: what the
melds in it come to, whether one of them holds a clean run, whether that is
enough to go down on, and what the hand would cost if it ended now. It reads
the hand as its owner has arranged it, which is why it can answer without
asking.

Anything a turn did can be taken back, last thing first: the melds in the
order they were laid, and after them the card taken off the discard pile. A
card off the stock never goes back. Putting it where it came from would let
a player look at the top of the stock and then decide not to have looked,
and there is no undoing knowing something.

Cards are seen to cross between a hand and the felt, whichever way they are
going. The thing that shows it is not told the direction: it is given cards
to watch and where they are now, and after the table has been drawn again it
finds them wherever they ended up. That is also why taking back a card off
the pile needs no animation of its own - the cards it was watching did not
move, so nothing travels.

The table fits the screen and never scrolls, which is what `fits` on the
page shell asks for: a card below the fold is a card nobody can reach. So
the board does not grow to hold more - what gives way as it fills is the
size of the cards laid on it.

Next: the end of a hand. Going out, the counting that follows, and the
finishing card finally doing whatever it is going to do.

Still open in the game: nothing sequences hands, so a game is one deal and
stops — `GameMode` is chosen when a table is opened, checked, and then read
by nobody. Nothing scores a hand, nothing doubles, nothing records a leaver's
loss. Tables live in memory, so a restart loses every table and every result
in it. Two of those wait on rules `docs/rules.md` still calls unsettled.

Still open around it: the Google link on the account page; rate limiting and
CAPTCHA on registration — it cannot require a credential, so those are its
only protection — and a mail sender, which unblocks password reset, email
confirmation and changing an address.

There is no test project. What has been checked was checked by harnesses
built against the real code and thrown away afterwards, which is why nothing
notices when one of them goes stale.
