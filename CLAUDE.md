# Rummy51

An Israeli Rummy 51 card game: ASP.NET Core API + Angular client.

## Working agreements

- **Never commit without being asked.** Finish the work, report what changed, and stop.
  Wait for an explicit go-ahead before `git commit`, and again before pushing.
- Take one step at a time. Explain a design before writing it, not after.
- Code, comments and commit messages are in English. Conversation is in Hebrew.

## Layout

```
Rummy51.Domain          entities and game rules      no project references
Rummy51.Application     use cases and contracts      -> Domain
Rummy51.Infrastructure  EF Core, Identity, impls     -> Application
Rummy51.Server          controllers, DI, pipeline    -> Application, Infrastructure
rummy51.client          Angular 22
```

Every reference points inward. `Domain` is deliberately kept free of project
references so the game rules can be tested without HTTP or a database; its only
NuGet dependency is `Microsoft.Extensions.Identity.Stores`, which `Player`
needs.

`Application` names what it requires as an interface; `Infrastructure` supplies
the implementation and registers it in `AddInfrastructure`.

## Running it

The two halves start **separately**. Running the server does not start Angular:
`ASPNETCORE_HOSTINGSTARTUPASSEMBLIES` was removed from `launchSettings.json` for
that reason. Do not add it back.

- Server: F5 in Visual Studio (`https` profile) — opens `/swagger`
- Client: `cd rummy51.client && npm start` — `https://localhost:57541`

The dev server proxies `/api` to `https://localhost:7115` (`src/proxy.conf.js`),
so no CORS configuration is needed in development.

`SpaRoot` and the `.esproj` ProjectReference are still in `Rummy51.Server.csproj`:
publish must keep bundling Angular into `wwwroot`.

**Visual Studio holds a lock on the server's output while it is running.** If a
build fails with MSB3027, the server is running — stop it, or build with
`-p:BaseOutputPath=<temp>/` to check compilation only.

## Database

PostgreSQL 18. The connection string lives in `appsettings.Development.json`
(local machine only, by the owner's decision).

`EFCore.NamingConventions` maps every identifier to `snake_case`, so the schema
reads without quoting. Identity's tables are renamed to the project's own
vocabulary in `Rummy51DbContext.OnModelCreating`:

```
players  roles  player_claims  player_logins  player_tokens  player_roles  role_claims
```

`__EFMigrationsHistory` keeps its default name on purpose — renaming it after a
migration is recorded leaves EF unable to find its own bookkeeping.

Migrations live in `Rummy51.Infrastructure/Persistence/Migrations`:

```bash
dotnet ef migrations add <Name> --project Rummy51.Infrastructure --startup-project Rummy51.Server --output-dir Persistence/Migrations
dotnet ef database update --project Rummy51.Infrastructure --startup-project Rummy51.Server
```

`Update-Database` also works from the Package Manager Console (default project
must be `Rummy51.Infrastructure`).

## Identity

`Player : IdentityUser<Guid>` lives in `Domain` — a player is a domain concept,
and the single Identity dependency is the accepted price for one table instead
of two kept in sync.

- **The email is the login identifier**: `UserName` is set to `Email`.
  `RequireUniqueEmail` is on, and `ix_players_normalized_email` is unique, so the
  database enforces it too rather than trusting application code alone.
- `DisplayName` is separate from `UserName`: free text, may be Hebrew, need not
  be unique. `UserName` is constrained by Identity's allowed characters.
- `EmailConfirmed` is set on registration **because nothing sends mail yet**.
  Revisit when there is a mail sender.
- Role tables exist but are unused. Add roles when something needs them.
- `PhoneNumber` is inherited and unused. Left in place: two nullable columns cost
  nothing, and removing them breaks parts of `UserManager`.
- Lockout is enforced: five failed sign-ins lock the account for five minutes.
- Passwords: PBKDF2/HMAC-SHA512, 100,000 iterations, 128-bit per-user salt,
  all packed into `password_hash`. Never handle a password outside `UserManager`.

## Authentication

**Cookies, not JWT.** Angular is served from the server's own `wwwroot`, so the
two are same-origin and the browser can carry the credential itself. That buys
three things a token would have had to work around: the cookie is `httpOnly`, so
cross-site scripting cannot read it; it is encrypted, so the client cannot read
the claims either; and it slides, so a session ends after idleness rather than at
a fixed moment. JWT would earn its place against a native mobile client or a
cross-domain API — a web app in a phone browser is neither.

The cookie is `httpOnly`, `Secure`, `SameSite=Lax`. Lax rather than Strict: it
still withholds the cookie from the cross-site POST that CSRF needs, without
making a player who arrives from an outside link look signed out.

Cookie authentication defaults to redirecting an unauthenticated caller to a
login page. `CookieAuthenticationSetup` replaces that with plain 401 and 403,
since a redirect to a page that does not exist reaches Angular as a stray 404.

Timings come from the `Authentication` section of configuration, never from
code: `IdleTimeoutMinutes` (sliding, every request restarts it) and
`RememberMeDays`. Sign-in verifies with `CheckPasswordSignInAsync` and issues the
cookie with `SignInAsync` as a separate step — that is what lets "remember me"
set its own lifetime instead of inheriting the idle timeout.

The Data Protection keys that encrypt the cookie live in `data_protection_keys`.
On disk they would be lost whenever the host is replaced, signing everyone out.

Because the cookie is opaque to the client, **`GET /api/auth/me`** is how Angular
learns who is signed in; call it at startup to discover a live session. It reads
the player back from the database rather than trusting the cookie's claims, so a
renamed or deleted account is noticed.

`InvalidCredentials` is returned for both an unknown address and a wrong
password. Distinguishing them would let anyone test which addresses are
registered.

## Error contract

**The server never sends display text.** Every failure is a stable code plus an
ordered array of parameters; the client owns the wording and can switch language
without asking the server.

```json
{
  "status": 400,
  "title": "ValidationFailed",
  "traceId": "...",
  "errors": [
    { "code": "PasswordTooShort", "parameters": ["8"] },
    { "code": "PasswordRequiresDigit", "parameters": [] }
  ]
}
```

`parameters` is always present, possibly empty. `title` and `type` are for
whoever reads the response by hand; nothing user-facing is built from them.

Three paths produce that shape, and all three must keep producing it:

| Path | Where |
|---|---|
| Expected failure | `Result<T>` → `ApiProblem.Result` in the controller |
| Model binding / DataAnnotations | `InvalidModelStateResponseFactory` in `Program.cs` |
| Unhandled exception | `GlobalExceptionHandler` |

Conventions:

- **Expected failures are returned, not thrown.** A taken email address is an
  answer, not a fault. `Result<T>` carries `FailureKind` so the controller can
  tell a conflict (409) from a rejected value (400).
- Validation codes name field and rule together (`EmailRequired`,
  `DisplayNameLength`) so they need no parameters and translate directly.
- A code is letters and digits only. `ModelStateErrors` enforces that: framework
  prose, such as a JSON parser's complaint, becomes `MalformedRequest` rather
  than reaching the client dressed as a code it could look up.
- The email doubles as the user name, so Identity reports one taken address
  twice. Only `DuplicateEmail` is reported.
- Request DTOs do **not** use the `required` modifier: `System.Text.Json` rejects
  a payload missing such a property before validation runs, and complains in
  English prose instead of a code.
- Identity's limits are read back from `IdentityOptions` when building
  parameters, so the code and the number can never disagree.
- An exception never reaches the client — message and stack trace tell an
  attacker about the internals and the player nothing. `traceId` links the
  response to the log entry.

## Client

Angular 22 runs **zoneless**; `zone.js` is not installed. State that the template
reads must be a `signal`, or an assignment from an async callback will never
render. Use the built-in `@if` / `@for` control flow.

Planned, not yet built: runtime i18n with the server's error codes as keys, and
Hebrew means **RTL** — dynamic `dir` and logical CSS properties
(`margin-inline-start`, not `margin-left`) from the start.

Client-side validation should mirror the server rules so most errors never make
the round trip.

## State of play

Done: project layout, player table, error contract, Swagger at `/swagger`, and
`/api/auth` — `register`, `login`, `logout`, `me`.

Next: the Angular side — sign-up and sign-in forms, an interceptor that treats a
401 as "session over", and the i18n map from error codes to Hebrew.

After that, rate limiting and CAPTCHA on registration. It cannot require a
credential, so those are its only real protection; everything else is behind
`[Authorize]`.
