namespace Rummy51.Server.Tests;

/// <summary>
/// Three paths produce the one shape and all three must keep producing it:
/// a Result turned into a problem by a controller, model binding refusing a
/// request before any of our code runs, and anything uncaught. They are
/// written in three places and nothing but this compares them.
/// </summary>
public class TheErrorContract(TheServer server) : IClassFixture<TheServer>
{
    private const string ProblemJson = "application/problem+json";

    [Fact]
    public async Task A_result_that_failed_comes_back_as_the_one_shape()
    {
        server.Accounts.Refusing(FailureKind.Unauthorized, "InvalidCredentials");

        var answer = await LoggingIn();

        Assert.Equal(ProblemJson, answer.MediaType);
        Assert.Equal("Unauthorized", answer.Title);
        Assert.Equal(["InvalidCredentials"], answer.Codes);
        Assert.NotNull(answer.TraceId);
    }

    /// <summary>
    /// Model binding rejects a request before any of our code runs, so its
    /// reply is reshaped rather than written.
    /// </summary>
    [Fact]
    public async Task So_does_a_request_the_framework_refused_before_we_saw_it()
    {
        var answer = await Registering("{}");

        Assert.Equal(HttpStatusCode.BadRequest, answer.Status);
        Assert.Equal(ProblemJson, answer.MediaType);
        Assert.Equal("ValidationFailed", answer.Title);
        Assert.NotNull(answer.TraceId);
    }

    [Fact]
    public async Task And_so_does_something_nothing_expected()
    {
        server.Accounts.Throwing = new InvalidOperationException("the database is on fire");

        try
        {
            var answer = await LoggingIn();

            Assert.Equal(HttpStatusCode.InternalServerError, answer.Status);
            Assert.Equal(ProblemJson, answer.MediaType);
            Assert.Equal("UnexpectedError", answer.Title);
            Assert.Equal(["UnexpectedError"], answer.Codes);
            Assert.NotNull(answer.TraceId);
        }
        finally
        {
            server.Accounts.Throwing = null;
        }
    }

    /// <summary>
    /// A message or a stack trace tells an attacker about the internals and
    /// tells the player nothing. The traceId is the thread back to the log.
    /// </summary>
    [Fact]
    public async Task And_nothing_of_the_exception_crosses_the_wire()
    {
        server.Accounts.Throwing = new InvalidOperationException("the database is on fire");

        try
        {
            using var caller = server.Caller();
            var answer = await caller.PostAsync("/api/auth/login", Credentials());

            Assert.DoesNotContain("on fire", await answer.Content.ReadAsStringAsync());
        }
        finally
        {
            server.Accounts.Throwing = null;
        }
    }

    /// <summary>
    /// Each rule in a request type carries its code as the ErrorMessage, so
    /// the field and the rule are named together and most need no parameters.
    /// </summary>
    [Fact]
    public async Task A_request_missing_everything_names_every_rule_it_broke()
    {
        var answer = await Registering("{}");

        // Every rule an empty body broke, not only the required ones: an
        // empty string is also not an address and is also too short.
        Assert.Equal(
            ["DisplayNameLength", "DisplayNameRequired", "EmailInvalid",
             "EmailRequired", "PasswordRequired"],
            answer.Codes.Order());
    }

    /// <summary>
    /// Framework prose must not reach the client dressed as a code it could
    /// look up: a JSON parser's complaint is English, and the client would
    /// show the key.
    /// </summary>
    [Fact]
    public async Task But_a_body_the_parser_choked_on_names_no_rule_at_all()
    {
        var answer = await Registering("{ not json");

        Assert.Equal(HttpStatusCode.BadRequest, answer.Status);
        Assert.Equal(["MalformedRequest"], answer.Codes);
    }

    [Fact]
    public async Task Every_code_is_letters_and_digits_and_nothing_else()
    {
        var answer = await Registering("{ not json");

        Assert.All(answer.Codes, code => Assert.True(code.All(char.IsAsciiLetterOrDigit), code));
    }

    /// <summary>Always present, possibly empty, so the client never checks.</summary>
    [Fact]
    public async Task Parameters_are_there_even_where_a_code_needs_none()
    {
        var answer = await Registering("{}");

        Assert.NotEmpty(answer.Errors);
        Assert.All(answer.Errors, error => Assert.Empty(error.Parameters));
    }

    [Fact]
    public async Task And_are_in_the_order_the_wording_uses_them()
    {
        server.Accounts.Refusing(
            FailureKind.Validation, Error.With("PasswordTooShort", "8", "128"));

        var answer = await LoggingIn();

        Assert.Equal(["8", "128"], answer.Errors[0].Parameters);
    }

    /// <summary>
    /// Result carries the kind so the controller picks the status, and the
    /// three controllers that each decided this for themselves now do not.
    /// </summary>
    [Theory]
    [InlineData(FailureKind.Validation, HttpStatusCode.BadRequest, "ValidationFailed")]
    [InlineData(FailureKind.Conflict, HttpStatusCode.Conflict, "Conflict")]
    [InlineData(FailureKind.Unauthorized, HttpStatusCode.Unauthorized, "Unauthorized")]
    public async Task The_reason_it_failed_picks_the_status_that_says_the_same(
        FailureKind kind, HttpStatusCode status, string title)
    {
        server.Accounts.Refusing(kind, "SomethingWasWrong");

        var answer = await LoggingIn();

        Assert.Equal(status, answer.Status);
        Assert.Equal(title, answer.Title);
    }

    private async Task<Answered> LoggingIn()
    {
        using var caller = server.Caller();

        return await Answered.Of(await caller.PostAsync("/api/auth/login", Credentials()));
    }

    private async Task<Answered> Registering(string json)
    {
        using var caller = server.Caller();

        return await Answered.Of(await caller.PostAsync("/api/auth/register", Answered.Sent(json)));
    }

    private static StringContent Credentials() =>
        Answered.Sent("""{ "email": "somebody@example.test", "password": "a password" }""");
}
