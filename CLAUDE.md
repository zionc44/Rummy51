# Rummy51

An Israeli-popular variant of Rummy (not an Israeli game — Rummikub is the
Israeli one, and it is different): ASP.NET Core API + Angular client,
multiplayer only.

## Working agreements

- **Never commit without being asked.** Finish, report, stop.
- One step at a time. Explain a design before writing it.
- Code, comments and commits in English. Conversation in Hebrew.

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

A build failing with MSB3027 means the server is running and holding its output.
Stop it, or build with `-p:BaseOutputPath=<temp>/` to check compilation only.

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

Tailwind, **logical properties only** — `ms`/`me`, `start`/`end`, never
`ml`/`mr`. That is what makes the layout mirror when the language changes; a
stray `ml-4` is only noticed by whoever reads English. Exception: a wrapper
around an email or password input is `dir="ltr"`, since both read left to right
in any language.

ngx-translate loads `public/i18n/<code>.json` at runtime. Hebrew is the default
and carries `dir="rtl"`. Adding a language: an entry in `LANGUAGES`, a widening
of `LanguageCode`, and the JSON file.

Server error codes are translation keys under `errors.`; the ordered parameters
become `{{0}}`, `{{1}}`. Forms validate the same rules as the server and express
failures as the same codes, so one table and one component serve both.

A session is restored before the first render (`provideAppInitializer`), and
guards run both ways — signed out away from `/lobby`, signed in away from the
forms.

## State of play

Done: project layout, player table, error contract, Swagger at `/swagger`,
`/api/auth` (`register`, `login`, `logout`, `me`), and the client through
sign-up, sign-in and a lobby stub.

Next: the lobby itself. Then rate limiting and CAPTCHA on registration — it
cannot require a credential, so those are its only protection — and a mail
sender, which unblocks password reset and email confirmation.
