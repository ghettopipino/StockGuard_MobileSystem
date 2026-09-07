using Resend;

var builder = WebApplication.CreateBuilder(args);

// ── RESEND ───────────────────────────────────────────────────────

builder.Services.AddOptions();

builder.Services.AddHttpClient<ResendClient>();

builder.Services.Configure<ResendClientOptions>(options =>
{
    options.ApiToken =
        Environment.GetEnvironmentVariable("RESEND_APITOKEN")
        ?? throw new InvalidOperationException(
            "RESEND_APITOKEN is not configured.");
});

builder.Services.AddTransient<IResend, ResendClient>();

var app = builder.Build();


// ── PASSWORD RESET EMAIL ─────────────────────────────────────────

app.MapPost(
    "/api/password-reset/send-code",
    async (
        PasswordResetEmailRequest request,
        IResend resend) =>
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Code))
        {
            return Results.BadRequest(
                new
                {
                    success = false,
                    message =
                        "Email and verification code are required."
                });
        }

        try
        {
            var message =
                new EmailMessage();

            message.From =
                "StockGuard <onboarding@resend.dev>";

            message.To.Add(
                request.Email.Trim().ToLower());

            message.Subject =
                "StockGuard Password Reset";

            message.HtmlBody =
                $"""
                <div style="
                    font-family:Arial,sans-serif;
                    max-width:520px;
                    margin:auto;
                    padding:24px;">

                    <h2>StockGuard Password Reset</h2>

                    <p>
                        We received a request to reset
                        your StockGuard account password.
                    </p>

                    <p>Your verification code is:</p>

                    <div style="
                        font-size:32px;
                        font-weight:bold;
                        letter-spacing:8px;
                        margin:24px 0;">
                        {request.Code}
                    </div>

                    <p>
                        This verification code expires
                        in 10 minutes.
                    </p>

                    <p>
                        If you did not request a password
                        reset, you can ignore this email.
                    </p>

                    <hr />

                    <small>
                        StockGuard · Equipment Monitoring
                        and Accountability
                    </small>

                </div>
                """;

            await resend.EmailSendAsync(
                message);

            return Results.Ok(
                new
                {
                    success = true,
                    message =
                        "Verification code sent."
                });
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"Email error: {ex.Message}");

            return Results.Problem(
                "Unable to send verification email.");
        }
    });

app.Run();


// ── REQUEST MODEL ─────────────────────────────────────────────────

public sealed class PasswordResetEmailRequest
{
    public string Email { get; set; } =
        string.Empty;

    public string Code { get; set; } =
        string.Empty;
}