# BulkyBook — Local Development Setup

This guide explains how to configure the SQL Server database, Mailjet email service, and Stripe payments for local development.

## Prerequisites

- Install the .NET SDK matching the `TargetFramework` in the web project's `.csproj` file.
- Install SQL Server or SQL Server Express, or obtain access to a compatible remote SQL Server database.
- Use Visual Studio with the ASP.NET and web development workload, or an editor with the .NET CLI.
- Create Mailjet and Stripe accounts to test email and payment features.

## 1. Get the project

Clone this repository and open its solution in Visual Studio. Restore NuGet packages when prompted. For CLI setup, run `dotnet restore` from the solution directory.

## 2. Create the development configuration

Create `appsettings.Development.json` in the web project directory, alongside `Program.cs` and `appsettings.json`.

Use the following template. Every credential below is a placeholder and must be replaced with your own value.

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "SQLConnection": "Server=YOUR_SERVER\\SQLEXPRESS;Database=BulkyBook;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;"
  },
  "Mailjet": {
    "ApiKey": "YOUR_MAILJET_API_KEY",
    "SecretKey": "YOUR_MAILJET_SECRET_KEY",
    "SenderEmail": "your-verified-sender@example.com",
    "SenderName": "BulkyBook Store"
  },
  "Stripe": {
    "PublishableKey": "pk_test_REPLACE_WITH_YOUR_KEY",
    "SecretKey": "sk_test_REPLACE_WITH_YOUR_KEY"
  }
}
```

Keep the configuration key names unchanged: the application expects these names. `TrustServerCertificate=True` in the example is intended for local development only.

## 3. Configure SQL Server

Set `ConnectionStrings:SQLConnection` to your database connection string.

| Value                      | Description                                                                                            |
| -------------------------- | ------------------------------------------------------------------------------------------------------ |
| `Server`                   | Your SQL Server instance, such as `localhost`, `.\\SQLEXPRESS` in JSON, or a remote database endpoint. |
| `Database`                 | The database name. The example uses `BulkyBook`.                                                       |
| `Integrated Security=True` | Use your Windows account for a local Windows-authenticated SQL Server connection.                      |
| `User Id` / `Password`     | Use these for SQL authentication instead of `Integrated Security=True`.                                |

For a remote SQL Server using SQL authentication, use this pattern and replace all placeholders:

```json
"SQLConnection": "Server=YOUR_DATABASE_ENDPOINT,1433;Database=BulkyBook;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;Encrypt=True;TrustServerCertificate=False;"
```

Use the actual port configured for your server. Ensure the database is reachable from your machine and that its certificate is trusted. Do not use your Windows instance name as an AWS database endpoint.

Prepare the database schema using the project's existing SQL scripts or migrations. If the repository contains Entity Framework Core migrations, apply them using the appropriate startup and migrations projects. Do not create a new initial migration just to run an existing project.

## 4. Configure Mailjet

1. Sign in to your Mailjet account.
2. Obtain your API key and secret key from API settings.
3. Register and validate the sender email address or sending domain in Mailjet for that API key.
4. Fill in the following settings:

| Configuration key     | Value                                                       |
| --------------------- | ----------------------------------------------------------- |
| `Mailjet:ApiKey`      | Your Mailjet API key.                                       |
| `Mailjet:SecretKey`   | Your Mailjet secret key. Keep this private.                 |
| `Mailjet:SenderEmail` | A sender email address validated in Mailjet.                |
| `Mailjet:SenderName`  | The display name recipients see, such as `BulkyBook Store`. |

Placeholder credentials will not send email. Email-dependent flows require working Mailjet configuration.

## 5. Configure Stripe

1. Sign in to the Stripe Dashboard and select a sandbox or test mode.
2. Copy the publishable key and secret key for the same test environment.
3. Set `Stripe:PublishableKey` to your `pk_test_...` key.
4. Set `Stripe:SecretKey` to your `sk_test_...` key.

Use test credentials for development and demo payments. Keep the secret key on the server; never expose it in browser code or commit it to Git. If the application implements webhooks, follow its webhook configuration separately; no webhook signing secret is shown in this configuration template.

## 6. Run the application

In Visual Studio, select the web project as the startup project, choose a launch profile with `ASPNETCORE_ENVIRONMENT` set to `Development`, and run the application.

Alternatively, open PowerShell in the web project directory and run:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --no-launch-profile
```

Open the URL printed in the application output. With the default ASP.NET Core configuration, `appsettings.Development.json` overrides matching values from `appsettings.json` when the environment is `Development`.

Verify database-backed pages, an email-dependent flow, and Stripe checkout in test mode. No default administrator credentials are specified by this guide; check the project's account initialization code if an administrator account is needed.

## 7. Keep credentials out of Git

Before adding real credentials, add these entries to the repository-root `.gitignore`:

```gitignore
appsettings.Development.json
appsettings.Production.json
```

These patterns match files with those names in subdirectories too. Keep a separate placeholder-only configuration example in the repository if desired.

If a configuration file is already tracked, `.gitignore` does not stop tracking it. Remove it from the Git index using its actual repository-relative path:

```bash
git rm --cached path/to/appsettings.Development.json
```

Replace `path/to/` with the actual folder path. This keeps the local file but removes it from the next commit. Previously committed credentials remain in Git history; rotate any exposed passwords or secret keys.

## Production configuration

For deployment, use the hosting provider's environment variables or secret settings. With the default ASP.NET Core configuration, environment variables override JSON settings. Use these exact variable names:

| Environment variable               | Corresponding configuration key   |
| ---------------------------------- | --------------------------------- |
| `ConnectionStrings__SQLConnection` | `ConnectionStrings:SQLConnection` |
| `Mailjet__ApiKey`                  | `Mailjet:ApiKey`                  |
| `Mailjet__SecretKey`               | `Mailjet:SecretKey`               |
| `Mailjet__SenderEmail`             | `Mailjet:SenderEmail`             |
| `Mailjet__SenderName`              | `Mailjet:SenderName`              |
| `Stripe__PublishableKey`           | `Stripe:PublishableKey`           |
| `Stripe__SecretKey`                | `Stripe:SecretKey`                |

Run the deployed application in `Production`. A production deployment does not load `appsettings.Development.json` by default. Demo deployments should continue using Stripe test credentials.

## Troubleshooting

| Problem                              | What to check                                                                                                     |
| ------------------------------------ | ----------------------------------------------------------------------------------------------------------------- |
| Cannot connect to SQL Server         | Server name, port, authentication, database permissions, network access, and TLS certificate trust.               |
| Development settings are not applied | File location and spelling, valid JSON, active environment, and overriding user secrets or environment variables. |
| Email is not sent                    | Mailjet API credentials, validated sender, account restrictions, and application logs.                            |
| Stripe checkout fails                | Both keys belong to the same test environment and configuration names match the application.                      |

## References

- [ASP.NET Core configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [Mailjet sender validation](https://dev.mailjet.com/docs/email-api/senders-domains/create-sender)
- [Stripe API authentication](https://docs.stripe.com/api/authentication)

This guide is based on the supplied development configuration. Exact SDK requirements, database initialization commands, and account seeding depend on the repository source.
