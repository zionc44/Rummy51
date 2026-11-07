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
number. `Dealing.Deal` is written and tested by construction, but nothing
reaches it yet.

Next: the deal, which is where secrecy starts — fourteen or fifteen cards have
to reach one player and nobody else.

Still open: the Google link on the account page; rate limiting
and CAPTCHA on registration — it cannot require a credential, so those are its
only protection — and a mail sender, which unblocks password reset, email
confirmation and changing an address.
