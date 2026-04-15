# Challenge Gate

A simple, configurable password challenge gate for .NET MVC applications. Ideal for protecting user research environments, staging sites, or non-production deployments where a simple "gate" is needed without a full user account system.

## Key Features
- **Easy Toggle**: Turn it on or off via configuration (e.g., disable in Production).
- **Self-Contained**: Middleware, Controller, and Razor View are all contained within a single Razor Class Library (RCL).
- **GOV.UK Design System**: Built-in support for GOV.UK Frontend (v6+), ensuring consistency with government standards.
- **Dynamic Routing**: The challenge page URL is fully configurable and mapped at runtime.
- **Seamless Integration**: Automatically adopts your host application's `_Layout.cshtml`.
- **Validation**: Uses **FluentValidation** for a clean, attribute-free validation experience.

## Security Features
- **Timing Attack Protection**: Uses constant-time comparison (`CryptographicOperations.FixedTimeEquals`) for password validation.
- **Signed & Encrypted Cookies**: Uses **ASP.NET Core Data Protection** to cryptographically sign the session cookie, preventing spoofing.
- **Automatic Invalidation**: The session cookie is tied to the current password. Changing the password in your configuration automatically invalidates all existing user sessions.

## Prerequisites
- **.NET 10.0 SDK** or higher.
- **FluentValidation** (configured automatically via the extensions).

## Background & Alternatives Considered

Before settling on this middleware approach, several other methods were evaluated and used historically to protect non-production environments. This solution was chosen to solve specific issues with those approaches:

*   **Public + robots.txt**: Simply relying on `/robots.txt` to limit indexing is "security by obscurity" and leaves the environment completely open to discovery and direct access.
*   **IP Allowlisting**: Restricting access to specific DfE VPN egress IPs or domestic IPs is brittle, hard to maintain for external User Research (UR) participants, and doesn't work well for remote teams.
*   **Entra ID (Azure AD) Integration**: Great for internal staff (e.g., `@education.gov.uk` accounts), but overly complex for external participants or rapid, temporary staging environments.
*   **HTTP Basic Auth**: Often blocked by strict corporate security policies (especially for UR participants using managed devices like Edge), resulting in them being unable to access the environments at all.
*   **Magic Links**: Built previously as a workaround for the Basic Auth blocking issues, but introduces significant architectural complexity (email sending, token expiration, database storage) for what should be a simple environment gate.

This **Challenge Gate** provides the perfect middle ground: it is cryptographically secure against casual snooping, works on all devices and browsers without triggering corporate auth blocks, and requires zero infrastructure (no databases, no email providers, no Entra config).

## Setup Instructions

### 1. Copy the Project
Copy the `src/ChallengeGate` folder into your solution and add it as a project reference.

### 2. Register Services
In your `Program.cs`, add the following:

```csharp
using ChallengeGate;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Challenge Gate Services (includes DataProtection and FluentValidation)
builder.Services.AddChallengeGate(builder.Configuration);

var app = builder.Build();

// ... existing middleware (e.g., UseStaticFiles, UseRouting) ...

// 2. Add the Middleware
// Recommended: After UseStaticFiles but before UseAuthorization
app.UseChallengeGate();

app.UseAuthorization();

// 3. Map the dynamic route
app.MapChallengeGate();

app.Run();
```

### 3. Configuration
Add the `ChallengeGate` section to your `appsettings.json`. You can provide different settings for different environments (e.g., set `Enabled: false` in `appsettings.Production.json`).

```json
{
  "ChallengeGate": {
    "Enabled": true,
    "Password": "your-secret-password",
    "CookieName": ".ChallengeGate.Auth",
    "CookieExpirationMinutes": 10080,
    "ChallengePath": "/challenge",
    "Layout": "_Layout",
    "Title": "Enter password",
    "BypassPaths": [ "/lib", "/css", "/js", "/assets", "/favicon.ico" ]
  }
}
```

| Setting                   | Description                                          | Default                                              |
|:--------------------------|:-----------------------------------------------------|:-----------------------------------------------------|
| `Enabled`                 | Toggles the gate on or off.                          | `false`                                              |
| `Password`                | The required password to bypass the gate.            | `null`                                               |
| `CookieName`              | Name of the authentication cookie.                   | `.ChallengeGate.Auth`                                |
| `CookieExpirationMinutes` | How long the access lasts before requiring re-entry. | `10080` (1 week)                                     |
| `ChallengePath`           | The URL path for the challenge form.                 | `/challenge`                                         |
| `Layout`                  | The Razor layout file the challenge page should use. | `_Layout`                                            |
| `Title`                   | The page title and `<h1>` for the challenge page.    | `Enter password`                                     |
| `BypassPaths`             | List of path prefixes that should always be allowed. | `["/lib", "/css", "/js", "/assets", "/favicon.ico"]` |


## Customization

### Styling with GOV.UK Frontend
The challenge page is built using GOV.UK Design System components. To ensure it looks correct in your host app:
1.  **Assets**: Ensure your app serves the GOV.UK Frontend CSS/JS and assets (fonts/images).
2.  **Pathing**: GDS CSS expects assets to be available at `/assets/`. 

See the `src/ChallengeGate.Web/ChallengeGate.Web.csproj` for a sample **MSBuild target** that automatically manages these assets using `npm`.

### Manual Extensions
Because this is delivered as a project, you can easily modify `ChallengeController.cs` or `Index.cshtml` to add extra logging, custom branding, or additional security checks (like rate limiting).

## Development & Testing
- **Tests**: The solution includes a comprehensive xUnit test suite in `tests/ChallengeGate.Tests`. Run them using `dotnet test`.
- **Demo**: Run the `src/ChallengeGate.Web` project to see the gate in action. Default password is set to `change-me-12345`.

## Contributing
Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
