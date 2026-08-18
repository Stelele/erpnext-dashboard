# Replace Auth0 with Local Session Auth — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove Auth0 and replace it with a self-hosted email + password login using opaque, never-expiring session tokens stored in PostgreSQL, plus email-based password setup/reset via SMTP and Admin/Viewer roles.

**Architecture:** A custom `AuthenticationHandler` validates a Bearer token against a `Sessions` table (SHA-256 hash only, no expiry). The handler emits claims including a `scope` claim derived from the user's `Role`, so the existing `RequireAuthorization` policies and `HasScopeHandler` keep working unchanged. `UserContextMiddleware` reads the `user_id` claim directly instead of an Auth0 namespace JSON claim. Password hashing uses ASP.NET's `PasswordHasher<User>`; password setup/reset uses one shared `PasswordResetTokens` table with a 24h TTL and a MailKit `IEmailSender`. Frontend swaps the Auth0 plugin for a local login page and token-in-localStorage auth.

**Tech Stack:** .NET 10, MediatR, EF Core + SQLite, ASP.NET Core `PasswordHasher<User>` (Microsoft.AspNetCore.App), MailKit, Vue 3, Pinia, vue-router, Dexie/IndexedDB

**Spec:** `docs/superpowers/specs/2026-08-14-remove-auth0-auth-design.md`

---

## Baseline

Verify before starting: `dotnet build` succeeds (currently succeeds with warnings) and `npm run build` succeeds in `frontend/`.

---

## Task 1: Domain model — Role enum, User changes, Session + PasswordResetToken

**Files:**
- Create: `backend/Domain/Users/Role.cs`
- Modify: `backend/Domain/Users/User.cs` (full rewrite)
- Create: `backend/Domain/Users/Session.cs`
- Create: `backend/Domain/Users/PasswordResetToken.cs`

- [ ] **Step 1: Create `backend/Domain/Users/Role.cs`**

```csharp
namespace Domain.Users;

public enum Role
{
    Viewer = 0,
    Admin = 1
}
```

- [ ] **Step 2: Rewrite `backend/Domain/Users/User.cs`**

Replace the whole file with:

```csharp
using Domain.Abstractions;
using Domain.Companies;

namespace Domain.Users;

public class User : Base
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Role Role { get; set; } = Role.Viewer;
    public string? PasswordHash { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }

    public List<Company> Companies { get; set; } = [];
    public List<Session> Sessions { get; set; } = [];
    public List<PasswordResetToken> PasswordResetTokens { get; set; } = [];

    public static User Create(
        string name,
        string email,
        Role role = Role.Viewer,
        List<Company>? companies = default)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Role = role,
            Companies = companies ?? []
        };

        return user;
    }
}
```

Note: `Auth0UserId` is gone.

- [ ] **Step 3: Create `backend/Domain/Users/Session.cs`**

```csharp
using Domain.Abstractions;

namespace Domain.Users;

public class Session : Base
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset LastUsedOn { get; set; }

    public static Session Create(Guid userId, string tokenHash)
    {
        return new Session
        {
            UserId = userId,
            TokenHash = tokenHash,
            LastUsedOn = DateTimeOffset.UtcNow
        };
    }
}
```

- [ ] **Step 4: Create `backend/Domain/Users/PasswordResetToken.cs`**

```csharp
using Domain.Abstractions;

namespace Domain.Users;

public class PasswordResetToken : Base
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresOn { get; set; }
    public DateTimeOffset? UsedOn { get; set; }

    public static PasswordResetToken Create(Guid userId, string tokenHash, DateTimeOffset expiresOn)
    {
        return new PasswordResetToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresOn = expiresOn
        };
    }

    public bool IsUsable(DateTimeOffset now) => UsedOn is null && now < ExpiresOn;
}
```

- [ ] **Step 5: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds (this is a no-op for callers yet).

- [ ] **Step 6: Commit**

```bash
git add backend/Domain/Users/
git commit -m "feat: add Role enum, session and password-reset domain entities"
```

---

## Task 2: EF Core entity configurations + DbContext

**Files:**
- Modify: `backend/Infrastructure/Models/UserEntity.cs` (full rewrite)
- Create: `backend/Infrastructure/Models/SessionEntity.cs`
- Create: `backend/Infrastructure/Models/PasswordResetTokenEntity.cs`
- Modify: `backend/Infrastructure/Models/DashboardDbContext.cs`

- [ ] **Step 1: Rewrite `backend/Infrastructure/Models/UserEntity.cs`**

Replace the whole file with:

```csharp
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class UserEntity : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(b => b.Id);

        builder
            .Property(b => b.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(b => b.Name)
            .IsRequired();

        builder
            .Property(b => b.Email)
            .IsRequired();

        builder
            .HasIndex(b => b.Email)
            .IsUnique();

        builder
            .Property(b => b.Role)
            .HasConversion<string>()
            .IsRequired();

        builder
            .Property(b => b.PasswordHash)
            .IsRequired(false);

        builder
            .Property(b => b.FailedLoginCount)
            .IsRequired();

        builder
            .Property(b => b.LockoutUntil)
            .IsRequired(false);

        builder
            .HasMany(b => b.Companies)
            .WithMany(b => b.Users);

        builder
            .HasMany(b => b.Sessions)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(b => b.PasswordResetTokens)
            .WithOne(b => b.User)
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
```

Note: the `Auth0UserId` property configuration is removed.

- [ ] **Step 2: Create `backend/Infrastructure/Models/SessionEntity.cs`**

```csharp
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class SessionEntity : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(b => b.Id);

        builder
            .Property(b => b.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(b => b.UserId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .HasIndex(b => b.TokenHash)
            .IsUnique();

        builder
            .Property(b => b.TokenHash)
            .IsRequired();

        builder
            .Property(b => b.LastUsedOn)
            .IsRequired();

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
```

- [ ] **Step 3: Create `backend/Infrastructure/Models/PasswordResetTokenEntity.cs`**

```csharp
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Models;

public class PasswordResetTokenEntity : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.HasKey(b => b.Id);

        builder
            .Property(b => b.Id)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .Property(b => b.UserId)
            .HasConversion(
                g => g.ToString().ToLowerInvariant(),
                s => Guid.Parse(s));

        builder
            .HasIndex(b => b.TokenHash)
            .IsUnique();

        builder
            .Property(b => b.TokenHash)
            .IsRequired();

        builder
            .Property(b => b.ExpiresOn)
            .IsRequired();

        builder
            .Property(b => b.UsedOn)
            .IsRequired(false);

        builder
            .Property(b => b.CreatedOn)
            .IsRequired();

        builder
            .Property(b => b.UpdatedOn)
            .IsRequired();
    }
}
```

- [ ] **Step 4: Update `backend/Infrastructure/Models/DashboardDbContext.cs`**

Add the two `DbSet`s and the two configuration registrations. The file becomes:

```csharp
using Domain.Abstractions;
using Domain.Companies;
using Domain.CompanyExpenseMappings;
using Domain.CompanySettings;
using Domain.ExpenseTypes;
using Domain.Sites;
using Domain.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Models;

public class DashboardDbContext(DbContextOptions<DashboardDbContext> options, IPublisher publisher) : DbContext(options)
{
    public DbSet<Site> Sites { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<ExpenseType> ExpenseTypes { get; set; }
    public DbSet<CompanyExpenseMapping> CompanyExpenseMappings { get; set; }
    public DbSet<CompanySettings> CompanySettings { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new SiteEntity().Configure(modelBuilder.Entity<Site>());
        new UserEntity().Configure(modelBuilder.Entity<User>());
        new CompanyEntity().Configure(modelBuilder.Entity<Company>());
        new ExpenseTypeEntity().Configure(modelBuilder.Entity<ExpenseType>());
        new CompanyExpenseMappingEntity().Configure(modelBuilder.Entity<CompanyExpenseMapping>());
        new CompanySettingsEntity().Configure(modelBuilder.Entity<CompanySettings>());
        new SessionEntity().Configure(modelBuilder.Entity<Session>());
        new PasswordResetTokenEntity().Configure(modelBuilder.Entity<PasswordResetToken>());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker.Entries<Base>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = entities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var _event in events)
            await publisher.Publish(_event, cancellationToken);

        foreach (var entity in entities)
            entity.ClearDomainEvents();

        return result;
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds.

- [ ] **Step 6: Commit**

```bash
git add backend/Infrastructure/Models/
git commit -m "feat: configure session and password-reset token entities"
```

---

## Task 3: EF Core migration

**Files:**
- Generated: `backend/Infrastructure/Migrations/` (new migration)

- [ ] **Step 1: Add the migration**

Run (from `backend/`):

```bash
dotnet ef migrations add AddLocalAuth --project Infrastructure --startup-project Host --output-dir Migrations
```

If `dotnet-ef` is not installed: `dotnet tool install --global dotnet-ef` first.

Expected: a new `YYYYMMDDHHMMSS_AddLocalAuth.cs` migration drops the `Auth0UserId` column, adds `Role`/`PasswordHash`/`FailedLoginCount`/`LockoutUntil` to `Users`, and creates `Sessions` and `PasswordResetTokens` tables with unique indexes on `TokenHash`. The `Snapshot` model is updated.

- [ ] **Step 2: Review the generated migration**

Open the new migration's `Up` method and confirm:
- `DropColumn(name: "Auth0UserId", table: "Users")`
- `AddColumn` for `Role` (non-nullable, `"Viewer"` default or mapped string), `PasswordHash` (nullable), `FailedLoginCount` (default 0), `LockoutUntil` (nullable)
- `CreateTable` for `Sessions` and `PasswordResetTokens` with FK cascade to Users

If any of these are missing, fix the migration by hand (it is checked-in, generated code).

- [ ] **Step 3: Build + verify migration applies**

Run: `dotnet build` in `backend/`
Then run: `dotnet ef migrations list --project Infrastructure --startup-project Host`
Expected: the new migration appears in the list.

- [ ] **Step 4: Commit**

```bash
git add backend/Infrastructure/Migrations/
git commit -m "feat: add EF migration for local auth tables"
```

---

## Task 4: Infrastructure — token helper, email sender, DI + MailKit package

**Files:**
- Create: `backend/Infrastructure/Auth/OpaqueToken.cs`
- Create: `backend/Infrastructure/Email/EmailMessage.cs`
- Create: `backend/Infrastructure/Email/IEmailSender.cs`
- Create: `backend/Infrastructure/Email/SmtpOptions.cs`
- Create: `backend/Infrastructure/Email/SmtpEmailSender.cs`
- Create: `backend/Infrastructure/Email/EmailTemplates.cs`
- Modify: `backend/Infrastructure/Infrastructure.csproj` (add MailKit)
- Modify: `backend/Infrastructure/DependancyInjection.cs`

- [ ] **Step 1: Create `backend/Infrastructure/Auth/OpaqueToken.cs`**

```csharp
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Auth;

public static class OpaqueToken
{
    public static string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
```

- [ ] **Step 2: Create `backend/Infrastructure/Email/EmailMessage.cs`**

```csharp
namespace Infrastructure.Email;

public record EmailMessage(string To, string Subject, string HtmlBody);
```

- [ ] **Step 3: Create `backend/Infrastructure/Email/IEmailSender.cs`**

```csharp
namespace Infrastructure.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create `backend/Infrastructure/Email/SmtpOptions.cs`**

```csharp
namespace Infrastructure.Email;

public class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
```

- [ ] **Step 5: Create `backend/Infrastructure/Email/SmtpEmailSender.cs`**

```csharp
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(
                options.Value.Host,
                options.Value.Port,
                options.Value.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                ct);

            if (!string.IsNullOrEmpty(options.Value.Username))
                await client.AuthenticateAsync(options.Value.Username, options.Value.Password, ct);

            var mime = new MimeMessage
            {
                From = { MailboxAddress.Parse(options.Value.From) },
                To = { MailboxAddress.Parse(message.To) },
                Subject = message.Subject,
                Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody()
            };

            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}", message.To);
            throw;
        }
    }
}
```

- [ ] **Step 6: Create `backend/Infrastructure/Email/EmailTemplates.cs`**

```csharp
namespace Infrastructure.Email;

public static class EmailTemplates
{
    public static EmailMessage PasswordSetup(string to, string name, string resetUrl) =>
        new(
            to,
            "Set your Njeremoto Dashboard password",
            $"<p>Hi {name},</p>" +
            $"<p>An account has been created for you on the Njeremoto Dashboard.</p>" +
            $"<p><a href=\"{resetUrl}\">Set your password</a>. This link expires in 24 hours.</p>");

    public static EmailMessage PasswordReset(string to, string name, string resetUrl) =>
        new(
            to,
            "Reset your Njeremoto Dashboard password",
            $"<p>Hi {name},</p>" +
            $"<p>Click the link below to reset your Njeremoto Dashboard password.</p>" +
            $"<p><a href=\"{resetUrl}\">Reset your password</a>. This link expires in 24 hours.</p>");
}
```

- [ ] **Step 7: Add MailKit to `backend/Infrastructure/Infrastructure.csproj`**

Add inside the `<ItemGroup>` that contains the Auth0 packages:

```xml
  <ItemGroup>
    <PackageReference Include="MailKit" Version="4.10.0" />
  </ItemGroup>
```

- [ ] **Step 8: Register email in `backend/Infrastructure/DependancyInjection.cs`**

Add to `AddInfrastructure`, right after `builder.Services.AddMemoryCache();`:

```csharp
        builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email:Smtp"));
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
```

Add the using: `using Infrastructure.Email;`

- [ ] **Step 9: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds.

- [ ] **Step 10: Commit**

```bash
git add backend/Infrastructure/
git commit -m "feat: add opaque token helper and SMTP email sender"
```

---

## Task 5: Application auth — commands, DTO, DI, password hasher

**Files:**
- Create: `backend/Application/Auth/LoginCommand.cs`
- Create: `backend/Application/Auth/LoginCommandHandler.cs`
- Create: `backend/Application/Auth/LogoutCommand.cs`
- Create: `backend/Application/Auth/LogoutCommandHandler.cs`
- Create: `backend/Application/Auth/ForgotPasswordCommand.cs`
- Create: `backend/Application/Auth/ForgotPasswordCommandHandler.cs`
- Create: `backend/Application/Auth/ResetPasswordCommand.cs`
- Create: `backend/Application/Auth/ResetPasswordCommandHandler.cs`
- Create: `backend/Application/Auth/PasswordResetTokenService.cs`
- Create: `backend/Application/Requests/LoginRequest.cs`
- Create: `backend/Application/Requests/ForgotPasswordRequest.cs`
- Create: `backend/Application/Requests/ResetPasswordRequest.cs`
- Modify: `backend/Application/DTOs/UserResponse.cs`
- Modify: `backend/Domain/Exceptions/Exceptions.cs`
- Modify: `backend/Application/DependancyInjection.cs`

- [ ] **Step 1: Create `backend/Application/Auth/LoginCommand.cs`**

```csharp
using Application.Abstractions;
using Application.DTOs;
using FluentValidation;

namespace Application.Auth;

public record LoginResponse(string Token, UserResponse User);

public record LoginCommand(string Email, string Password) : ICommand<LoginResponse>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

- [ ] **Step 2: Create `backend/Application/Auth/LoginCommandHandler.cs`**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Auth;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth;

public class LoginCommandHandler(
    DashboardDbContext db,
    IPasswordHasher<User> passwordHasher
) : ICommandHandler<LoginCommand, LoginResponse>
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Companies)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null || user.PasswordHash is null)
            throw new UnauthorizedException("Invalid email or password.");

        if (user.LockoutUntil is { } lockoutUntil && lockoutUntil > DateTimeOffset.UtcNow)
            throw new UnauthorizedException("Account locked. Try again later.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount += 1;
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.FailedLoginCount = 0;
                user.LockoutUntil = DateTimeOffset.UtcNow + LockoutDuration;
            }
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Invalid email or password.");
        }

        user.FailedLoginCount = 0;
        user.LockoutUntil = null;

        var token = OpaqueToken.Generate();
        db.Sessions.Add(Session.Create(user.Id, OpaqueToken.Hash(token)));
        await db.SaveChangesAsync(ct);

        return new LoginResponse(token, UserResponse.FromDomain(user));
    }
}
```

- [ ] **Step 3: Create `backend/Application/Auth/LogoutCommand.cs`**

```csharp
using Application.Abstractions;
using MediatR;

namespace Application.Auth;

public record LogoutCommand(string Token) : ICommand<Unit>;
```

- [ ] **Step 4: Create `backend/Application/Auth/LogoutCommandHandler.cs`**

```csharp
using Application.Abstractions;
using Infrastructure.Auth;
using Infrastructure.Models;
using MediatR;

namespace Application.Auth;

public class LogoutCommandHandler(DashboardDbContext db) : ICommandHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken ct)
    {
        await db.Sessions
            .Where(s => s.TokenHash == OpaqueToken.Hash(request.Token))
            .ExecuteDeleteAsync(ct);

        return Unit.Value;
    }
}
```

- [ ] **Step 5: Create `backend/Application/Auth/ForgotPasswordCommand.cs`**

```csharp
using Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Application.Auth;

public record ForgotPasswordCommand(string Email) : ICommand<Unit>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

- [ ] **Step 6: Create `backend/Application/Auth/ForgotPasswordCommandHandler.cs`**

```csharp
using Application.Abstractions;
using Infrastructure.Email;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Auth;

public class ForgotPasswordCommandHandler(
    DashboardDbContext db,
    IPasswordResetTokenService resetTokenService,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<ForgotPasswordCommandHandler> logger
) : ICommandHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        // Always return success; don't reveal whether the email exists.
        if (user is null)
            return Unit.Value;

        try
        {
            var token = await resetTokenService.CreateAsync(user.Id, ct);
            var frontendUrl = configuration["App:FrontendUrl"]
                ?? throw new InvalidOperationException("App:FrontendUrl is null");
            var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";
            await emailSender.SendAsync(EmailTemplates.PasswordReset(user.Email, user.Name, resetUrl), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password reset email for {Email}", user.Email);
        }

        return Unit.Value;
    }
}
```

- [ ] **Step 7: Create `backend/Application/Auth/ResetPasswordCommand.cs`**

```csharp
using Application.Abstractions;
using Application.DTOs;
using FluentValidation;

namespace Application.Auth;

public record ResetPasswordCommand(string Token, string NewPassword) : ICommand<LoginResponse>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(8);
    }
}
```

- [ ] **Step 8: Create `backend/Application/Auth/ResetPasswordCommandHandler.cs`**

```csharp
using Application.Abstractions;
using Application.DTOs;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Auth;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Auth;

public class ResetPasswordCommandHandler(
    DashboardDbContext db,
    IPasswordHasher<User> passwordHasher
) : ICommandHandler<ResetPasswordCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var tokenHash = OpaqueToken.Hash(request.Token);
        var resetToken = await db.PasswordResetTokens
            .Include(t => t.User)
                .ThenInclude(u => u.Companies)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct)
            ?? throw new UnauthorizedException("Invalid or expired reset token.");

        if (!resetToken.IsUsable(DateTimeOffset.UtcNow))
            throw new UnauthorizedException("Invalid or expired reset token.");

        var user = resetToken.User;
        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutUntil = null;

        resetToken.UsedOn = DateTimeOffset.UtcNow;

        // Revoke all existing sessions so the old token stops working.
        await db.Sessions.Where(s => s.UserId == user.Id).ExecuteDeleteAsync(ct);

        var token = OpaqueToken.Generate();
        db.Sessions.Add(Session.Create(user.Id, OpaqueToken.Hash(token)));

        await db.SaveChangesAsync(ct);

        return new LoginResponse(token, UserResponse.FromDomain(user));
    }
}
```

- [ ] **Step 9: Create `backend/Application/Auth/PasswordResetTokenService.cs`**

```csharp
using Domain.Users;
using Infrastructure.Auth;
using Infrastructure.Models;

namespace Application.Auth;

public interface IPasswordResetTokenService
{
    Task<string> CreateAsync(Guid userId, CancellationToken ct);
}

public class PasswordResetTokenService(DashboardDbContext db) : IPasswordResetTokenService
{
    public async Task<string> CreateAsync(Guid userId, CancellationToken ct)
    {
        var raw = OpaqueToken.Generate();
        db.PasswordResetTokens.Add(PasswordResetToken.Create(
            userId,
            OpaqueToken.Hash(raw),
            DateTimeOffset.UtcNow.AddHours(24)));
        await db.SaveChangesAsync(ct);
        return raw;
    }
}
```

- [ ] **Step 10: Create the request DTOs**

`backend/Application/Requests/LoginRequest.cs`:

```csharp
using FluentValidation;

namespace Application.Requests;

public record LoginRequest(string Email, string Password);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
```

`backend/Application/Requests/ForgotPasswordRequest.cs`:

```csharp
using FluentValidation;

namespace Application.Requests;

public record ForgotPasswordRequest(string Email);

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

`backend/Application/Requests/ResetPasswordRequest.cs`:

```csharp
using FluentValidation;

namespace Application.Requests;

public record ResetPasswordRequest(string Token, string NewPassword);

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
```

- [ ] **Step 11: Update `backend/Application/DTOs/UserResponse.cs` to include Role**

Replace the whole file with:

```csharp
using Domain.Users;

namespace Application.DTOs;

public record UserResponse(
    Guid Id,
    string Name,
    string Email,
    Role Role,
    List<Guid> Companies
)
{
    public static UserResponse FromDomain(User user) =>
        new(
            user.Id,
            user.Name,
            user.Email,
            user.Role,
            [.. user.Companies.Select(c => c.Id)]
        );
}
```

- [ ] **Step 12: Add `UnauthorizedException` to `backend/Domain/Exceptions/Exceptions.cs`**

Replace the whole file with:

```csharp
namespace Domain.Exceptions;

public class DuplicateDomainMemberException(string message) : Exception(message);
public class NotFoundException(string message) : Exception(message);
public class UnauthorizedException(string message) : Exception(message);
```

- [ ] **Step 13: Register auth services in `backend/Application/DependancyInjection.cs`**

Update the `AddApplication` method. After `builder.Services.AddScoped<IUserContext>(...)`, add:

```csharp
            builder.Services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
            builder.Services.AddSingleton<IPasswordHasher<User>>(new PasswordHasher<User>());
```

Add usings: `using Application.Auth;`, `using Domain.Users;`, `using Microsoft.AspNetCore.Identity;`

- [ ] **Step 14: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds.

- [ ] **Step 15: Commit**

```bash
git add backend/Application/ backend/Domain/Exceptions/
git commit -m "feat: add login, logout, forgot and reset password commands"
```

---

## Task 6: Session authentication handler + middleware + exception cleanup

**Files:**
- Create: `backend/Endpoints/Authentication/SessionAuthenticationOptions.cs`
- Create: `backend/Endpoints/Authentication/SessionAuthenticationHandler.cs`
- Create: `backend/Endpoints/Authentication/SessionAuthDefaults.cs`
- Create: `backend/Endpoints/Authentication/RolePermissions.cs`
- Modify: `backend/Endpoints/DependancyInjection.cs`
- Modify: `backend/Host/Middleware/UserContextMiddleware.cs`
- Modify: `backend/Host/Middleware/GlobalExceptionMiddleware.cs`

- [ ] **Step 1: Create `backend/Endpoints/Authentication/SessionAuthenticationOptions.cs`**

```csharp
namespace Api.Authentication;

public class SessionAuthenticationOptions : AuthenticationSchemeOptions;
```

- [ ] **Step 2: Create `backend/Endpoints/Authentication/SessionAuthDefaults.cs`**

```csharp
namespace Api.Authentication;

public static class SessionAuthDefaults
{
    public const string AuthenticationScheme = "Session";
}
```

- [ ] **Step 3: Create `backend/Endpoints/Authentication/RolePermissions.cs`**

```csharp
using Domain.Users;

namespace Api.Authentication;

public static class RolePermissions
{
    private static readonly string[] AllPermissions =
    [
        Permissions.ReadUsers, Permissions.CreateUsers, Permissions.UpdateUsers, Permissions.DeleteUsers,
        Permissions.ReadCompanies, Permissions.CreateCompanies, Permissions.UpdateCompanies, Permissions.DeleteCompanies,
        Permissions.ReadSites, Permissions.CreateSites, Permissions.UpdateSites, Permissions.DeleteSites,
        Permissions.ReadExpenses, Permissions.CreateExpenses, Permissions.UpdateExpenses, Permissions.DeleteExpenses,
    ];

    private static readonly string[] ViewerPermissions =
    [
        Permissions.ReadUsers, Permissions.ReadCompanies, Permissions.ReadSites, Permissions.ReadExpenses,
    ];

    public static IReadOnlyList<string> For(Role role) =>
        role == Role.Admin ? AllPermissions : ViewerPermissions;
}
```

- [ ] **Step 4: Create `backend/Endpoints/Authentication/SessionAuthenticationHandler.cs`**

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Infrastructure.Auth;
using Infrastructure.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Api.Authentication;

public class SessionAuthenticationHandler(
    IOptionsMonitor<SessionAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DashboardDbContext db)
    : AuthenticationHandler<SessionAuthenticationOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var session = await db.Sessions
            .AsNoTracking()
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TokenHash == OpaqueToken.Hash(token));

        if (session is null || session.User is null)
            return AuthenticateResult.Fail("Invalid session token.");

        await db.Sessions
            .Where(s => s.Id == session.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.LastUsedOn, DateTimeOffset.UtcNow));

        var claims = new[]
        {
            new Claim("user_id", session.User.Id.ToString()),
            new Claim(ClaimTypes.Name, session.User.Name),
            new Claim(ClaimTypes.Email, session.User.Email),
            new Claim("role", session.User.Role.ToString()),
            new Claim("scope", string.Join(' ', RolePermissions.For(session.User.Role))),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
```

- [ ] **Step 5: Swap the auth scheme in `backend/Endpoints/DependancyInjection.cs`**

Replace the `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` block with:

```csharp
        builder.Services
            .AddAuthentication(SessionAuthDefaults.AuthenticationScheme)
            .AddScheme<SessionAuthenticationOptions, SessionAuthenticationHandler>(
                SessionAuthDefaults.AuthenticationScheme, _ => { });
```

Remove the using `using Microsoft.AspNetCore.Authentication.JwtBearer;`. Keep `using Api.Authentication;`.

- [ ] **Step 6: Rewrite `backend/Host/Middleware/UserContextMiddleware.cs`**

Replace the whole file with:

```csharp
using Application.Users;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Host.Middleware;

public class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserContext userContext, DashboardDbContext db)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userIdClaim = context.User.FindFirst("user_id");
            if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                var user = await db.Users
                    .AsNoTracking()
                    .Include(u => u.Companies)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user != null)
                {
                    userContext.UserId = user.Id;
                    userContext.CompanyIds = user.Companies.Select(c => c.Id).ToList();
                }
            }
        }

        await _next(context);
    }
}
```

- [ ] **Step 7: Remove Auth0 handling from `backend/Host/Middleware/GlobalExceptionMiddleware.cs`**

Remove `using Auth0.Core.Exceptions;`. In `InvokeAsync`, delete the `if (ex is ErrorApiException auth0Ex) ... else ...` split and keep the generic log:

```csharp
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleExceptionAsync(context, ex);
        }
```

In `GetStatusCodeAndPayload`, remove the `ErrorApiException` case and add:

```csharp
            UnauthorizedException uex => (
                StatusCodes.Status401Unauthorized,
                new Problem
                {
                    Title = "Unauthorized",
                    Status = 401,
                    Detail = uex.Message
                }
            ),
```

Keep the existing `UnauthorizedAccessException` case.

- [ ] **Step 8: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds.

- [ ] **Step 9: Commit**

```bash
git add backend/Endpoints/ backend/Host/Middleware/
git commit -m "feat: replace Auth0 JWT with opaque session auth handler"
```

---

## Task 7: Auth endpoints + wire into MapApi

**Files:**
- Create: `backend/Endpoints/Endpoints/AuthEndpoints.cs`
- Modify: `backend/Endpoints/Tags.cs`
- Modify: `backend/Endpoints/DependancyInjection.cs`

- [ ] **Step 1: Create `backend/Endpoints/Endpoints/AuthEndpoints.cs`**

```csharp
using Application.Auth;
using Application.DTOs;
using Application.Requests;
using Application.Users;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Api.Endpoints;

public static class AuthEndpoints
{
    public static WebApplication MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (LoginRequest request, ISender mediator) =>
        {
            var result = await mediator.Send(new LoginCommand(request.Email, request.Password));
            return Results.Ok(result);
        })
         .WithName("Login")
         .WithDisplayName("Login")
         .Accepts<LoginRequest>("application/json")
         .Produces<LoginResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status401Unauthorized)
         .WithTags(Tags.Auth)
         .AllowAnonymous();

        app.MapPost("/auth/logout", async (HttpContext http, ISender mediator) =>
        {
            var token = ExtractToken(http.Request.Headers.Authorization.ToString());
            if (token is not null)
                await mediator.Send(new LogoutCommand(token));
            return Results.NoContent();
        })
         .WithName("Logout")
         .WithDisplayName("Logout")
         .Produces(StatusCodes.Status204NoContent)
         .WithTags(Tags.Auth)
         .RequireAuthorization();

        app.MapGet("/auth/me", async (HttpContext http, ISender mediator) =>
        {
            var userIdClaim = http.User.FindFirst("user_id");
            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Results.Unauthorized();

            var user = await mediator.Send(new GetUserByIdQuery(userId));
            return user is not null ? Results.Ok(user) : Results.Unauthorized();
        })
         .WithName("GetCurrentUser")
         .WithDisplayName("GetCurrentUser")
         .Produces<UserResponse>(StatusCodes.Status200OK)
         .WithTags(Tags.Auth)
         .RequireAuthorization();

        app.MapPost("/auth/forgot-password", async (ForgotPasswordRequest request, ISender mediator) =>
        {
            await mediator.Send(new ForgotPasswordCommand(request.Email));
            return Results.Ok();
        })
         .WithName("ForgotPassword")
         .WithDisplayName("ForgotPassword")
         .Accepts<ForgotPasswordRequest>("application/json")
         .Produces(StatusCodes.Status200OK)
         .WithTags(Tags.Auth)
         .AllowAnonymous();

        app.MapPost("/auth/reset-password", async (ResetPasswordRequest request, ISender mediator) =>
        {
            var result = await mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
            return Results.Ok(result);
        })
         .WithName("ResetPassword")
         .WithDisplayName("ResetPassword")
         .Accepts<ResetPasswordRequest>("application/json")
         .Produces<LoginResponse>(StatusCodes.Status200OK)
         .Produces(StatusCodes.Status401Unauthorized)
         .WithTags(Tags.Auth)
         .AllowAnonymous();

        return app;
    }

    private static string? ExtractToken(string authHeader)
    {
        if (string.IsNullOrEmpty(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader["Bearer ".Length..].Trim();
    }
}
```

- [ ] **Step 2: Add the Auth tag in `backend/Endpoints/Tags.cs`**

Add a line inside the class:

```csharp
    public static string Auth = "Auth";
```

- [ ] **Step 3: Wire into `backend/Endpoints/DependancyInjection.cs`**

In `MapApi`, add `.MapAuthEndpoints()` to the chain:

```csharp
        app
            .MapCompanyEndpoints()
            .MapSitesEndpoints()
            .MapUsersEndpoints()
            .MapExpenseEndpoints()
            .MapThemeEndpoints()
            .MapAuthEndpoints();
```

- [ ] **Step 4: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds.

- [ ] **Step 5: Commit**

```bash
git add backend/Endpoints/
git commit -m "feat: add auth endpoints (login, logout, me, password reset)"
```

---

## Task 8: User provisioning — role-aware create, email setup, role update, no Auth0

**Files:**
- Modify: `backend/Application/Users/CreateUserCommand.cs`
- Modify: `backend/Application/Requests/CreateUserRequest.cs`
- Modify: `backend/Application/Users/CreateUserCommandHandler.cs`
- Modify: `backend/Application/Users/DeleteUserCommandHandler.cs`
- Create: `backend/Application/Users/UpdateUserRoleCommand.cs`
- Create: `backend/Application/Users/UpdateUserRoleCommandHandler.cs`
- Create: `backend/Application/Requests/UpdateUserRoleRequest.cs`
- Modify: `backend/Endpoints/Endpoints/UsersEndpoints.cs`

- [ ] **Step 1: Update `backend/Application/Users/CreateUserCommand.cs`**

Replace the record with:

```csharp
public record CreateUserCommand(
    string Name,
    string Email,
    Role Role,
    List<Guid> Companies
) : ICommand<Guid>;
```

Add `using Domain.Users;` and a validator rule in the existing validator:

```csharp
        RuleFor(x => x.Role)
            .IsInEnum();
```

- [ ] **Step 2: Update `backend/Application/Requests/CreateUserRequest.cs`**

Replace the record with:

```csharp
public record CreateUserRequest(
    string Name,
    string Email,
    Role Role,
    List<Guid> Companies
);
```

Add `using Domain.Users;` and to the validator:

```csharp
        RuleFor(x => x.Role).IsInEnum();
```

- [ ] **Step 3: Rewrite `backend/Application/Users/CreateUserCommandHandler.cs`**

Replace the whole file with:

```csharp
using Application.Abstractions;
using Application.Auth;
using Domain.Exceptions;
using Domain.Users;
using Infrastructure.Email;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Users;

public class CreateUserCommandHandler(
    DashboardDbContext db,
    IPasswordResetTokenService resetTokenService,
    IEmailSender emailSender,
    IConfiguration configuration,
    ILogger<CreateUserCommandHandler> logger
) : ICommandHandler<CreateUserCommand, Guid>
{
    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var userExists = await db.Users
            .AnyAsync(u => u.Email == request.Email, cancellationToken);

        if (userExists)
            throw new DuplicateDomainMemberException($"A user with email {request.Email} already exists.");

        var companies = await db.Companies
            .Where(c => request.Companies.Contains(c.Id))
            .ToListAsync(cancellationToken);

        var user = User.Create(request.Name, request.Email, request.Role, companies);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var token = await resetTokenService.CreateAsync(user.Id, cancellationToken);
            var frontendUrl = configuration["App:FrontendUrl"]
                ?? throw new InvalidOperationException("App:FrontendUrl is null");
            var resetUrl = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";
            await emailSender.SendAsync(EmailTemplates.PasswordSetup(user.Email, user.Name, resetUrl), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password setup email for user {Email}", user.Email);
        }

        return user.Id;
    }
}
```

- [ ] **Step 4: Rewrite `backend/Application/Users/DeleteUserCommandHandler.cs`**

Replace the whole file with:

```csharp
using Application.Abstractions;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class DeleteUserCommandHandler(DashboardDbContext db) : ICommandHandler<DeleteUserCommand, Unit>
{
    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Not found user: {request.Id}");

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

Sessions and reset tokens for the user are removed by the FK cascade configured in Task 2.

- [ ] **Step 5: Create `backend/Application/Users/UpdateUserRoleCommand.cs`**

```csharp
using Application.Abstractions;
using Domain.Users;
using MediatR;

namespace Application.Users;

public record UpdateUserRoleCommand(Guid UserId, Role Role) : ICommand<Unit>;
```

- [ ] **Step 6: Create `backend/Application/Users/UpdateUserRoleCommandHandler.cs`**

```csharp
using Application.Abstractions;
using Infrastructure.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Users;

public class UpdateUserRoleCommandHandler(DashboardDbContext db) : ICommandHandler<UpdateUserRoleCommand, Unit>
{
    public async Task<Unit> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"Not found user: {request.UserId}");

        if (user.Role == request.Role)
            return Unit.Value;

        user.Role = request.Role;

        // Force the role change to take effect immediately.
        await db.Sessions.Where(s => s.UserId == user.Id).ExecuteDeleteAsync(cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

- [ ] **Step 7: Create `backend/Application/Requests/UpdateUserRoleRequest.cs`**

```csharp
using Domain.Users;
using FluentValidation;

namespace Application.Requests;

public record UpdateUserRoleRequest(Role Role);

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role).IsInEnum();
    }
}
```

- [ ] **Step 8: Update `backend/Endpoints/Endpoints/UsersEndpoints.cs`**

Update the `CreateUser` handler to pass the role:

```csharp
            var command = new CreateUserCommand(request.Name, request.Email, request.Role, request.Companies);
```

Add a role-change endpoint after `CreateUser`:

```csharp
        app.MapPatch("/users/{id}/role", async (Guid id, UpdateUserRoleRequest request, ISender mediator) =>
        {
            await mediator.Send(new UpdateUserRoleCommand(id, request.Role));
            return Results.NoContent();
        })
         .WithName("UpdateUserRole")
         .WithDisplayName("UpdateUserRole")
         .Accepts<UpdateUserRoleRequest>("application/json")
         .Produces(StatusCodes.Status204NoContent)
         .Produces(StatusCodes.Status404NotFound)
         .WithTags(Tags.Users)
         .RequireAuthorization(Permissions.UpdateUsers);
```

Add `using Application.Requests;` (already present).

- [ ] **Step 9: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds.

- [ ] **Step 10: Commit**

```bash
git add backend/Application/Users/ backend/Application/Requests/ backend/Endpoints/Endpoints/UsersEndpoints.cs
git commit -m "feat: role-aware user provisioning with email password setup"
```

---

## Task 9: Remove Auth0 provisioner, packages, and config

**Files:**
- Delete: `backend/Infrastructure/Auth0/Auth0UserProvisioner.cs`
- Modify: `backend/Infrastructure/Infrastructure.csproj` (remove Auth0 packages)
- Modify: `backend/Endpoints/Api.csproj` (remove JwtBearer package)
- Modify: `backend/Infrastructure/DependancyInjection.cs` (remove provisioner registration)
- Modify: `backend/Host/appsettings.json`
- Modify: `backend/docker-compose.yml`
- Modify: `.github/workflows/deploy.yml`
- Modify: `frontend/.env`
- Modify: `backend/README.md`, `frontend/README.md`, `README.md` (remove Auth0 references)

- [ ] **Step 1: Delete the Auth0 provisioner**

```bash
rm backend/Infrastructure/Auth0/Auth0UserProvisioner.cs
```

- [ ] **Step 2: Remove the Auth0 package references**

From `backend/Infrastructure/Infrastructure.csproj`, remove:

```xml
    <PackageReference Include="Auth0.AuthenticationApi" Version="7.43.0" />
    <PackageReference Include="Auth0.ManagementApi" Version="7.43.0" />
```

From `backend/Endpoints/Api.csproj`, remove:

```xml
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.11" />
```

- [ ] **Step 3: Remove the provisioner registration from `backend/Infrastructure/DependancyInjection.cs`**

Delete the whole `builder.Services.AddSingleton(sp => { ... Auth0UserProvisioner ... });` block and remove `using Infrastructure.Auth0;`.

- [ ] **Step 4: Update `backend/Host/appsettings.json`**

Remove the `Identity` and `Auth0` sections. Add:

```json
  "App": {
    "FrontendUrl": "https://dashboard.njeremoto.net"
  },
  "Email": {
    "Smtp": {
      "Host": "<Enter as user secret>",
      "Port": 587,
      "Username": "<Enter as user secret>",
      "Password": "<Enter as user secret>",
      "From": "<Enter as user secret>",
      "EnableSsl": true
    }
  },
```

- [ ] **Step 5: Update `backend/docker-compose.yml`**

Remove `Identity__ClientSecret`, `Auth0__Domain`, `Auth0__Audience`, `Auth0__Apps__ERP-Dashboard` env lines. Add:

```yaml
      - App__FrontendUrl={{APP_FRONTEND_URL}}
      - Email__Smtp__Host={{SMTP_HOST}}
      - Email__Smtp__Port={{SMTP_PORT}}
      - Email__Smtp__Username={{SMTP_USERNAME}}
      - Email__Smtp__Password={{SMTP_PASSWORD}}
      - Email__Smtp__From={{SMTP_FROM}}
```

- [ ] **Step 6: Update `.github/workflows/deploy.yml`**

In the frontend build step, remove the three `-e "s|{{auth0_...}}|$AUTH0_...|"` sed lines and remove the `AUTH0_DOMAIN` / `AUTH0_CLIENT_ID` / `AUTH0_AUDIENCE` env entries.

In the docker-compose placeholder replacement step, replace the three Auth0 `sed` lines with:

```bash
            -e 's|{{APP_FRONTEND_URL}}|${{ secrets.APP_FRONTEND_URL }}|g' \
            -e 's|{{SMTP_HOST}}|${{ secrets.SMTP_HOST }}|g' \
            -e 's|{{SMTP_PORT}}|587|g' \
            -e 's|{{SMTP_USERNAME}}|${{ secrets.SMTP_USERNAME }}|g' \
            -e 's|{{SMTP_PASSWORD}}|${{ secrets.SMTP_PASSWORD }}|g' \
            -e 's|{{SMTP_FROM}}|${{ secrets.SMTP_FROM }}|g' \
```

- [ ] **Step 7: Update `frontend/.env`**

Remove the `VITE_AUTH0_DOMAIN`, `VITE_AUTH0_CLIENT_ID`, `VITE_AUTH0_AUDIENCE` lines. Keep `VITE_API_URL={{api_url}}`.

- [ ] **Step 8: Update READMEs**

Remove Auth0 references from `README.md`, `backend/README.md`, `frontend/README.md` (Tech Stack "Auth: Auth0" rows, Auth0 setup/credential instructions).

- [ ] **Step 9: Build**

Run: `dotnet build` in `backend/`
Expected: succeeds with no references to Auth0.

- [ ] **Step 10: Verify no Auth0 references remain in backend source**

Run: `rg -n "Auth0|auth0" backend/ --glob '!bin/**' --glob '!obj/**' --glob '!Migrations/**'`
Expected: no matches (Migrations may still contain `Auth0UserId` in old snapshots — that is expected and fine).

- [ ] **Step 11: Commit**

```bash
git add -A backend frontend/.env .github/workflows/deploy.yml README.md
git commit -m "chore: remove Auth0 provisioner, packages, and configuration"
```

---

## Task 10: Backend tests — session auth factory + AuthTests + stub email

**Files:**
- Modify: `backend/Tests/IntegrationTestFactory.cs`
- Modify: `backend/Tests/UsersTests.cs` (CreateUserRequest signature)
- Create: `backend/Tests/StubEmailSender.cs`
- Create: `backend/Tests/SessionAuthTestFactory.cs`
- Create: `backend/Tests/AuthTests.cs`
- Delete: `backend/Tests/TestAuth0UserProvisioner.cs`

- [ ] **Step 1: Update `backend/Tests/IntegrationTestFactory.cs`**

In `ConfigureAppConfiguration`, remove the four `Auth0:...` / `Identity:...` entries and add:

```csharp
                ["App:FrontendUrl"] = "http://localhost:5173",
```

In `ConfigureServices`:
- Remove `services.AddSingleton<Auth0UserProvisioner, TestAuth0UserProvisioner>();` and `using Infrastructure.Auth0;`
- Add after the R2 stub:

```csharp
            services.AddSingleton<IEmailSender, StubEmailSender>();
```

- Add `using Infrastructure.Email;`
- Add this method to the class (in addition to the existing `CreateClient`):

```csharp
    public HttpClient CreateClientWithoutAuth()
    {
        return base.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }
```

- Extend `ResetDatabaseAsync` to clear the new tables:

```csharp
        db.PasswordResetTokens.RemoveRange(db.PasswordResetTokens);
        db.Sessions.RemoveRange(db.Sessions);
```

- [ ] **Step 2: Create `backend/Tests/StubEmailSender.cs`**

```csharp
using Infrastructure.Email;

namespace Tests;

public class StubEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        Sent.Add(message);
        return Task.CompletedTask;
    }
}
```

- [ ] **Step 3: Create `backend/Tests/SessionAuthTestFactory.cs`**

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Auth;
using Domain.Users;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class SessionAuthTestFactory : IntegrationTestFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication("Session");
        });
    }

    public async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password, Role role = Role.Admin)
    {
        var client = CreateClientWithoutAuth();
        var response = await client.PostAsJsonAsync("/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result!.Token);
        return client;
    }
}
```

- [ ] **Step 4: Update `backend/Tests/UsersTests.cs`**

The `CreateUserRequest` now takes `Role` as the third parameter. Update the constructor calls (lines with `new CreateUserRequest(name, email, [])`) to `new CreateUserRequest(name, email, Domain.Users.Role.Viewer, [])`. Add `using Domain.Users;`.

- [ ] **Step 5: Delete `backend/Tests/TestAuth0UserProvisioner.cs`**

```bash
rm backend/Tests/TestAuth0UserProvisioner.cs
```

- [ ] **Step 6: Create `backend/Tests/AuthTests.cs`**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Auth;
using Application.Requests;
using Domain.Users;
using Infrastructure.Email;
using Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class AuthTests : IClassFixture<SessionAuthTestFactory>
{
    private readonly SessionAuthTestFactory _factory;

    public AuthTests(SessionAuthTestFactory factory) => _factory = factory;

    private async Task ResetAsync() => await _factory.ResetDatabaseAsync();

    private async Task<Guid> SeedUserAsync(string email, string password, Role role)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        var user = User.Create("Test User", email, role);
        user.PasswordHash = hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user.Id;
    }

    private async Task<HttpClient> LoginAsync(string email = "admin@test.com", string password = "TestPass123!")
    {
        await SeedUserAsync(email, password, Role.Admin);
        return await _factory.CreateAuthenticatedClientAsync(email, password);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "TestPass123!" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.Equal("admin@test.com", result.User.Email);
        Assert.Equal(Role.Admin, result.User.Role);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_LocksAccountAfterFiveFailures()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);
        var client = _factory.CreateClientWithoutAuth();

        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "wrong" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        var locked = await client.PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "TestPass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, locked.StatusCode);
    }

    [Fact]
    public async Task Login_WithLockedAccount_Returns401()
    {
        await ResetAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var user = User.Create("Locked", "locked@test.com", Role.Admin);
            user.PasswordHash = hasher.HashPassword(user, "TestPass123!");
            user.LockoutUntil = DateTimeOffset.UtcNow.AddMinutes(15);
            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/login", new { email = "locked@test.com", password = "TestPass123!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithValidSessionToken_Succeeds()
    {
        await ResetAsync();
        var client = await LoginAsync();
        var response = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithGarbageToken_Returns401()
    {
        await ResetAsync();
        var client = _factory.CreateClientWithoutAuth();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "garbage");
        var response = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesSession()
    {
        await ResetAsync();
        var client = await LoginAsync();

        var logout = await client.PostAsync("/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var response = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ViewerToken_CannotCallWriteEndpoint()
    {
        await ResetAsync();
        await SeedUserAsync("viewer@test.com", "TestPass123!", Role.Viewer);
        var client = await _factory.CreateAuthenticatedClientAsync("viewer@test.com", "TestPass123!", Role.Viewer);

        var read = await client.GetAsync("/users");
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        var write = await client.PostAsJsonAsync("/users", new CreateUserRequest("New", "new@test.com", Role.Viewer, []));
        Assert.Equal(HttpStatusCode.Forbidden, write.StatusCode);
    }

    [Fact]
    public async Task AdminToken_CanCallWriteEndpoint()
    {
        await ResetAsync();
        var client = await LoginAsync();
        var response = await client.PostAsJsonAsync("/users", new CreateUserRequest("New", "new@test.com", Role.Viewer, []));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_SendsEmail_WithTokenLink()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "admin@test.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sender = _factory.Services.GetRequiredService<IEmailSender>() as StubEmailSender;
        Assert.NotNull(sender);
        var email = Assert.Single(sender!.Sent);
        Assert.Equal("admin@test.com", email.To);
        Assert.Contains("/reset-password?token=", email.HtmlBody);
    }

    [Fact]
    public async Task ForgotPassword_ForUnknownEmail_StillReturns200()
    {
        await ResetAsync();
        var response = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "nobody@test.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_SetsNewPassword_AndRevokesOldSessions()
    {
        await ResetAsync();
        var oldClient = await LoginAsync();

        var req = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "admin@test.com" });
        req.EnsureSuccessStatusCode();

        var sender = _factory.Services.GetRequiredService<IEmailSender>() as StubEmailSender;
        var token = sender!.Sent.Single().HtmlBody.Split("token=")[1].Trim();

        var client = _factory.CreateClientWithoutAuth();
        var reset = await client.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        var oldGet = await oldClient.GetAsync("/users");
        Assert.Equal(HttpStatusCode.Unauthorized, oldGet.StatusCode);

        var login = await client.PostAsJsonAsync("/auth/login", new { email = "admin@test.com", password = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithUsedToken_Fails()
    {
        await ResetAsync();
        await SeedUserAsync("admin@test.com", "TestPass123!", Role.Admin);

        var req = await _factory.CreateClientWithoutAuth()
            .PostAsJsonAsync("/auth/forgot-password", new { email = "admin@test.com" });
        req.EnsureSuccessStatusCode();

        var sender = _factory.Services.GetRequiredService<IEmailSender>() as StubEmailSender;
        var token = sender!.Sent.Single().HtmlBody.Split("token=")[1].Trim();

        var client = _factory.CreateClientWithoutAuth();
        var first = await client.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "NewPass123!" });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/auth/reset-password", new { token, newPassword = "Another123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
    }
}
```

- [ ] **Step 7: Run the tests**

Run: `dotnet test` in `backend/`
Expected: all tests pass, including the existing `UsersTests`, `CompaniesTests`, `SitesTests`, `ExpenseTests`, `CachingTests` (they still use the `Test` scheme), plus the new `AuthTests`.

If `dotnet test` fails to compile, fix compile errors in the modified test files before proceeding.

- [ ] **Step 8: Commit**

```bash
git add backend/Tests/
git commit -m "test: add session auth integration tests and stub email sender"
```

---

## Task 11: Frontend — remove Auth0 plugin, env, and authGuard

**Files:**
- Modify: `frontend/package.json`
- Modify: `frontend/src/main.ts`
- Modify: `frontend/src/routes/index.ts`
- Create: `frontend/src/guards/auth.ts`

- [ ] **Step 1: Remove `@auth0/auth0-vue`**

Edit `frontend/package.json`: remove `"@auth0/auth0-vue": "^2.5.0",` from `dependencies`.

- [ ] **Step 2: Rewrite `frontend/src/main.ts`**

Replace the whole file with:

```ts
import "./style.css";
import App from "./App.vue";
import { createApp } from "vue";
import { router } from "./routes";
import { createPinia } from "pinia";
import ui from "@nuxt/ui/vue-plugin";
import { addCollection } from "@iconify/vue";
import lucide from "@iconify-json/lucide/icons.json";

addCollection(lucide);

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);
app.use(ui);

app.mount("#app");
```

Note: Pinia is installed before the router so the auth guard can use the store.

- [ ] **Step 3: Create `frontend/src/guards/auth.ts`**

```ts
import type { RouteLocationNormalized } from "vue-router";
import { useAuthStore } from "@/stores/AuthStore";

export function authGuard(to: RouteLocationNormalized) {
  const authStore = useAuthStore();

  if (to.meta.public) {
    return authStore.accessToken ? { path: "/" } : true;
  }

  return authStore.accessToken ? true : { path: "/login" };
}
```

- [ ] **Step 4: Update `frontend/src/routes/index.ts`**

Replace the whole file with:

```ts
import { createRouter, createWebHistory } from "vue-router";

import { authGuard } from "@/guards/auth";

const LoginView = () => import("@/views/LoginView.vue");
const ResetPasswordView = () => import("@/views/ResetPasswordView.vue");
const OverviewView = () => import("@/views/OverviewView.vue");
const ExpensesView = () => import("@/views/ExpensesView.vue");
const SalesView = () => import("@/views/SalesView.vue");
const StockView = () => import("@/views/StockView.vue");

export const router = createRouter({
  routes: [
    {
      path: "/login",
      name: "Login",
      component: LoginView,
      meta: { public: true },
    },
    {
      path: "/reset-password",
      name: "ResetPassword",
      component: ResetPasswordView,
      meta: { public: true },
    },
    {
      path: "/",
      name: "Overview",
      beforeEnter: authGuard,
      component: OverviewView,
    },
    {
      path: "/expenses",
      name: "Expenses",
      beforeEnter: authGuard,
      component: ExpensesView,
    },
    {
      path: "/sales",
      name: "Sales",
      beforeEnter: authGuard,
      component: SalesView,
    },
    {
      path: "/stock",
      name: "Stock",
      beforeEnter: authGuard,
      component: StockView,
    },
    {
      path: "/:pathMatch(.*)*",
      redirect: "/",
    },
  ],
  history: createWebHistory(),
});
```

- [ ] **Step 5: Verify `frontend/.env` has no Auth0 vars** (done in Task 9 Step 7).

- [ ] **Step 6: Build**

Run: `npm install` then `npm run build` in `frontend/`
Expected: builds (may still fail to typecheck because AuthStore still imports `@auth0/auth0-vue` — fixed in Task 12; if `npm run build` passes, continue).

- [ ] **Step 7: Commit**

```bash
git add frontend/package.json frontend/package-lock.json frontend/src/main.ts frontend/src/routes/index.ts frontend/src/guards/
git commit -m "feat: replace Auth0 guard with local route guard"
```

---

## Task 12: Frontend — AuthStore rewrite, API client, login/reset pages, App bootstrap

**Files:**
- Modify: `frontend/src/stores/AuthStore.ts` (full rewrite)
- Modify: `frontend/src/services/api/index.ts`
- Modify: `frontend/src/services/cache/CachedApiClient.ts`
- Modify: `frontend/src/App.vue`
- Create: `frontend/src/views/LoginView.vue`
- Create: `frontend/src/views/ResetPasswordView.vue`
- Modify: `frontend/src/services/api/schema.ts` (regenerate or manual patch)

- [ ] **Step 1: Rewrite `frontend/src/stores/AuthStore.ts`**

Replace the whole file with:

```ts
import { defineStore } from "pinia";
import { ref } from "vue";
import { computed } from "vue";
import type { components } from "@/services/api/schema";
import { CachedApiClient } from "@/services/cache/CachedApiClient";
import { ApiSingleton } from "@/services/api";
import { getCacheDB } from "@/services/db";

const SELECTED_COMPANY_KEY = "selectedCompany";
const TOKEN_KEY = "authToken";
const USER_KEY = "authUser";

function safeGetItem(key: string): string | null {
  try {
    return localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeSetItem(key: string, value: string): void {
  try {
    localStorage.setItem(key, value);
  } catch {
    // Ignore in private browsing / storage disabled
  }
}

function safeRemoveItem(key: string): void {
  try {
    localStorage.removeItem(key);
  } catch {
    // Ignore
  }
}

export const useAuthStore = defineStore("authStore", () => {
  const selectedCompany = ref<string>("");
  const companies = ref<components["schemas"]["CompanyResponse"][]>([]);
  const siteUrl = ref("");
  const siteToken = ref("");
  const logo = ref("/logo.png");
  const logoUrls = ref<Record<string, string>>({});

  const company = computed(() => {
    if (
      selectedCompany.value &&
      companies.value?.find((c) => c.name === selectedCompany.value)
    ) {
      return selectedCompany.value;
    }
    return companies.value?.[0]?.name || "";
  });

  const url = computed(() => siteUrl.value);
  const token = computed(() => siteToken.value);

  const showSwitcher = computed(() => {
    return companies.value.length > 1;
  });

  const givenName = ref("");
  const email = ref("");
  const userId = ref("");
  const role = ref<components["schemas"]["UserResponse"]["role"]>("Viewer");
  const accessToken = ref(safeGetItem(TOKEN_KEY) ?? "");
  const user = ref<components["schemas"]["UserResponse"]>();

  let _loggingOut = false;

  async function loadSiteData(siteId: string) {
    const client = CachedApiClient.getInstance();
    const site = await client.getSite(siteId);
    if (site) {
      siteUrl.value = site.url;
      siteToken.value = site.apiToken;
    }
  }

  async function fetchLogoUrl(siteId: string, companyName: string): Promise<string> {
    const cacheKey = `${siteId}:${companyName}`;
    if (logoUrls.value[cacheKey]) return logoUrls.value[cacheKey];

    const client = CachedApiClient.getInstance();
    const url = await client.getSiteLogo(siteId, companyName);
    logoUrls.value[cacheKey] = url;
    return url;
  }

  async function loadCurrentLogo() {
    const currentCompany = companies.value.find((c) => c.name === company.value);
    if (currentCompany?.siteId) {
      logo.value = await fetchLogoUrl(currentCompany.siteId, company.value);
    }
  }

  async function loadAllLogos() {
    await Promise.all(
      companies.value
        .filter((c) => c.siteId)
        .map((c) => fetchLogoUrl(c.siteId, c.name)),
    );
  }

  function storeSession(token: string, u: components["schemas"]["UserResponse"]) {
    accessToken.value = token;
    safeSetItem(TOKEN_KEY, token);
    safeSetItem(USER_KEY, JSON.stringify(u));
    givenName.value = u.name;
    email.value = u.email;
    userId.value = u.id;
    role.value = u.role;
    user.value = u;
  }

  function clearSession() {
    accessToken.value = "";
    safeRemoveItem(TOKEN_KEY);
    safeRemoveItem(USER_KEY);
    givenName.value = "";
    email.value = "";
    userId.value = "";
    role.value = "Viewer";
    user.value = undefined;
    selectedCompany.value = "";
    safeRemoveItem(SELECTED_COMPANY_KEY);
  }

  async function login(loginEmail: string, password: string) {
    const api = await ApiSingleton.getInstance();
    const { data, error } = await api.POST("/auth/login", {
      body: { email: loginEmail, password },
    });
    if (error || !data) throw new Error("Invalid email or password.");
    storeSession(data.token, data.user);
    await update();
  }

  async function update() {
    const stored = safeGetItem(TOKEN_KEY);
    if (!stored) {
      clearSession();
      return;
    }
    accessToken.value = stored;

    try {
      const client = CachedApiClient.getInstance();
      const me = await client.getCurrentUser();
      if (!me) {
        clearSession();
        return;
      }
      storeSession(stored, me);

      if (me.companies?.length) {
        companies.value = await client.getUserCompanies();

        const persisted = safeGetItem(SELECTED_COMPANY_KEY);
        if (persisted && companies.value.find((c) => c.name === persisted)) {
          selectedCompany.value = persisted;
        }

        const selected = companies.value.find(
          (c) => c.name === selectedCompany.value,
        ) ?? companies.value[0];
        if (selected) {
          await Promise.all([
            (async () => { await loadCurrentLogo(); await loadAllLogos(); })(),
            loadSiteData(selected.siteId),
          ]);
        }
      }
    } catch (error) {
      console.error("Error restoring session:", error);
    }
  }

  async function triggerLogout() {
    if (_loggingOut) return;
    _loggingOut = true;

    const currentToken = accessToken.value;
    if (currentToken) {
      try {
        const api = await ApiSingleton.getInstance();
        await api.POST("/auth/logout", {});
      } catch {
        // ignore
      }
    }

    clearSession();

    try {
      await getCacheDB().delete();
      await getCacheDB().open();
    } catch {
      // ignore
    }

    window.location.href = "/login";
  }

  async function switchCompany(
    companyName: string,
    onDataRefresh: () => Promise<void>,
  ) {
    const previous = selectedCompany.value;
    selectedCompany.value = companyName;
    safeSetItem(SELECTED_COMPANY_KEY, companyName);

    const selected = companies.value.find((c) => c.name === companyName);
    if (selected) {
      await Promise.all([
        loadCurrentLogo(),
        loadSiteData(selected.siteId),
      ]);
    }

    try {
      await onDataRefresh();
    } catch (error) {
      selectedCompany.value = previous;
      safeSetItem(SELECTED_COMPANY_KEY, previous);
      throw error;
    }
  }

  return {
    companies,
    token,
    url,
    logo,
    logoUrls,
    company,
    showSwitcher,
    givenName,
    email,
    userId,
    role,
    accessToken,
    user,
    selectedCompany,
    update,
    switchCompany,
    login,
    triggerLogout,
    storeSession,
    clearSession,
  };
});
```

- [ ] **Step 2: Rewrite `frontend/src/services/api/index.ts`**

Replace the whole file with:

```ts
import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { useAuthStore } from "@/stores/AuthStore";

export type Client = ReturnType<typeof createClient<paths>>;

function createAuthFetch(): typeof globalThis.fetch {
  let redirecting = false;

  return async (input: Request) => {
    const authStore = useAuthStore();

    const headers = new Headers(input.headers);
    if (!headers.has("Authorization") && authStore.accessToken) {
      headers.set("Authorization", `Bearer ${authStore.accessToken}`);
    }

    const req = new Request(input, { headers });
    const response = await fetch(req);

    if (response.status === 401 && !redirecting) {
      redirecting = true;
      authStore.clearSession();
      if (window.location.pathname !== "/login") {
        window.location.href = "/login";
      }
    }

    return response;
  };
}

export class ApiSingleton {
  private static instance: Client | null = null;

  public static async getInstance() {
    if (this.instance) return this.instance;

    const api = createClient<paths>({
      baseUrl: import.meta.env.VITE_API_URL,
      fetch: createAuthFetch(),
    });

    this.instance = api;

    return api;
  }

  public static reset() {
    this.instance = null;
  }
}
```

- [ ] **Step 3: Add `getCurrentUser` to `frontend/src/services/cache/CachedApiClient.ts`**

Add this method to the class (after `getUser`):

```ts
  async getCurrentUser(): Promise<UserResponse | undefined> {
    const api = await this.ensureApi();
    const { data, error } = await api.GET("/auth/me", {});
    if (!error && data) return data;
    return undefined;
  }
```

- [ ] **Step 4: Update `frontend/src/App.vue` bootstrapping**

Replace the `onBeforeMount` body with:

```ts
onBeforeMount(async () => {
    await authStore.update();

    if (!authStore.accessToken) return;

    const cacheClient = CachedApiClient.getInstance();
    await cacheClient.init();
    await cacheClient.bootstrap(authStore.userId);

    startSync();
    update();

    const currentCompany = authStore.companies.find(
        (c) => c.name === authStore.company
    );
    if (currentCompany) {
        await loadAndApply(currentCompany.id);
    }
});
```

- [ ] **Step 5: Create `frontend/src/views/LoginView.vue`**

```vue
<script setup lang="ts">
import { ref } from "vue";
import { useAuthStore } from "@/stores/AuthStore";

const authStore = useAuthStore();
const email = ref("");
const password = ref("");
const error = ref("");
const loading = ref(false);

async function submit() {
  error.value = "";
  if (!email.value || !password.value) {
    error.value = "Enter your email and password.";
    return;
  }

  loading.value = true;
  try {
    await authStore.login(email.value, password.value);
    window.location.href = "/";
  } catch {
    error.value = "Invalid email or password.";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="login-page">
    <form class="login-card" @submit.prevent="submit">
      <h1>Njeremoto Dashboard</h1>

      <label>
        Email
        <input
          v-model="email"
          type="email"
          autocomplete="username"
          required
        />
      </label>

      <label>
        Password
        <input
          v-model="password"
          type="password"
          autocomplete="current-password"
          required
        />
      </label>

      <p v-if="error" class="login-error">{{ error }}</p>

      <button type="submit" :disabled="loading">
        {{ loading ? "Signing in…" : "Sign in" }}
      </button>

      <RouterLink to="/reset-password" class="forgot-link">
        Forgot password?
      </RouterLink>
    </form>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f8fafc;
  padding: 16px;
}
.login-card {
  width: 100%;
  max-width: 360px;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 32px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.login-card h1 {
  font-size: 20px;
  text-align: center;
  margin: 0 0 8px;
}
.login-card label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
}
.login-card input {
  padding: 10px 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 14px;
}
.login-card button {
  padding: 12px;
  border: none;
  border-radius: 8px;
  background: #0f172a;
  color: #fff;
  font-size: 15px;
  cursor: pointer;
}
.login-card button:disabled {
  opacity: 0.6;
}
.login-error {
  color: #dc2626;
  font-size: 14px;
  margin: 0;
}
.forgot-link {
  text-align: center;
  font-size: 14px;
  color: #2563eb;
}
</style>
```

- [ ] **Step 6: Create `frontend/src/views/ResetPasswordView.vue`**

```vue
<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute } from "vue-router";
import { ApiSingleton } from "@/services/api";
import { useAuthStore } from "@/stores/AuthStore";

const route = useRoute();
const token = computed(() => (route.query.token as string) || "");
const isReset = computed(() => Boolean(token.value));

const email = ref("");
const password = ref("");
const confirm = ref("");
const message = ref("");
const error = ref("");
const loading = ref(false);

async function submit() {
  error.value = "";
  message.value = "";

  if (!isReset.value) {
    if (!email.value) {
      error.value = "Enter your email.";
      return;
    }
    loading.value = true;
    try {
      const api = await ApiSingleton.getInstance();
      await api.POST("/auth/forgot-password", { body: { email: email.value } });
      message.value = "If an account exists for that email, a reset link has been sent.";
    } finally {
      loading.value = false;
    }
    return;
  }

  if (password.value.length < 8) {
    error.value = "Password must be at least 8 characters.";
    return;
  }
  if (password.value !== confirm.value) {
    error.value = "Passwords do not match.";
    return;
  }

  loading.value = true;
  try {
    const api = await ApiSingleton.getInstance();
    const { data, error: err } = await api.POST("/auth/reset-password", {
      body: { token: token.value, newPassword: password.value },
    });
    if (err || !data) {
      error.value = "This reset link is invalid or expired.";
      return;
    }
    useAuthStore().storeSession(data.token, data.user);
    window.location.href = "/";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="login-page">
    <form class="login-card" @submit.prevent="submit">
      <h1>{{ isReset ? "Set your password" : "Reset your password" }}</h1>

      <template v-if="isReset">
        <label>
          New password
          <input v-model="password" type="password" autocomplete="new-password" required />
        </label>
        <label>
          Confirm password
          <input v-model="confirm" type="password" autocomplete="new-password" required />
        </label>
      </template>
      <template v-else>
        <label>
          Email
          <input v-model="email" type="email" autocomplete="username" required />
        </label>
      </template>

      <p v-if="error" class="login-error">{{ error }}</p>
      <p v-if="message" class="login-message">{{ message }}</p>

      <button type="submit" :disabled="loading">
        {{ loading ? "Please wait…" : (isReset ? "Save password" : "Send reset link") }}
      </button>

      <RouterLink to="/login" class="forgot-link">Back to sign in</RouterLink>
    </form>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f8fafc;
  padding: 16px;
}
.login-card {
  width: 100%;
  max-width: 360px;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 32px;
  display: flex;
  flex-direction: column;
  gap: 16px;
}
.login-card h1 {
  font-size: 20px;
  text-align: center;
  margin: 0 0 8px;
}
.login-card label {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 14px;
}
.login-card input {
  padding: 10px 12px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  font-size: 14px;
}
.login-card button {
  padding: 12px;
  border: none;
  border-radius: 8px;
  background: #0f172a;
  color: #fff;
  font-size: 15px;
  cursor: pointer;
}
.login-card button:disabled {
  opacity: 0.6;
}
.login-error {
  color: #dc2626;
  font-size: 14px;
  margin: 0;
}
.login-message {
  color: #16a34a;
  font-size: 14px;
  margin: 0;
}
.forgot-link {
  text-align: center;
  font-size: 14px;
  color: #2563eb;
}
</style>
```

- [ ] **Step 7: Regenerate the API schema**

Run the backend (`dotnet run --project backend/Host`), then:

```bash
npm run codegen
```

in `frontend/`. This regenerates `frontend/src/services/api/schema.ts` with the new `/auth/*` paths, the `PATCH /users/{id}/role` endpoint, `Role` on `UserResponse`, and the new request/response schemas.

**If the backend cannot be started locally** (e.g., missing R2 secrets), apply this manual patch to `frontend/src/services/api/schema.ts` instead:

- Add a `role` property to `UserResponse`:

```ts
        UserResponse: {
            /** Format: uuid */
            id: string;
            name: string;
            email: string;
            role: "Viewer" | "Admin";
            companies: string[];
        };
```

- Add `role: "Viewer" | "Admin";` to `CreateUserRequest`:

```ts
        CreateUserRequest: {
            name: string;
            email: string;
            role: "Viewer" | "Admin";
            companies: string[];
        };
```

- Add these paths after the `/users/{userId}/companies/{companyId}` entry (line ~138 in the file, before `/api/expense-types`):

```ts
    "/auth/login": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["Login"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/auth/logout": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["Logout"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/auth/me": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: operations["GetCurrentUser"];
        put?: never;
        post?: never;
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/auth/forgot-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["ForgotPassword"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
    "/auth/reset-password": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get?: never;
        put?: never;
        post: operations["ResetPassword"];
        delete?: never;
        options?: never;
        head?: never;
        patch?: never;
        trace?: never;
    };
```

- Change the `/users/{id}` path entry so `patch` points to `UpdateUserRole`:

```ts
    "/users/{id}": {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        get: operations["GetUserById"];
        put?: never;
        post?: never;
        delete: operations["DeleteUser"];
        options?: never;
        head?: never;
        patch: operations["UpdateUserRole"];
        trace?: never;
    };
```

- Add schemas to `components.schemas` (after `MappingItemRequest`):

```ts
        LoginResponse: {
            token: string;
            user: components["schemas"]["UserResponse"];
        };
```

- Add these operations to the `operations` interface (at the end of the file, after `UpdateUserRole` if it exists — otherwise after `CreateUser`):

```ts
    Login: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["LoginRequest"];
            };
        };
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["LoginResponse"];
                };
            };
            /** @description Unauthorized */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["Problem"];
                };
            };
        };
    };
    Logout: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description No Content */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    GetCurrentUser: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody?: never;
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["UserResponse"];
                };
            };
        };
    };
    ForgotPassword: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["ForgotPasswordRequest"];
            };
        };
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
    ResetPassword: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["ResetPasswordRequest"];
            };
        };
        responses: {
            /** @description OK */
            200: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["LoginResponse"];
                };
            };
            /** @description Unauthorized */
            401: {
                headers: {
                    [name: string]: unknown;
                };
                content: {
                    "application/json": components["schemas"]["Problem"];
                };
            };
        };
    };
    UpdateUserRole: {
        parameters: {
            query?: never;
            header?: never;
            path?: never;
            cookie?: never;
        };
        requestBody: {
            content: {
                "application/json": components["schemas"]["UpdateUserRoleRequest"];
            };
        };
        responses: {
            /** @description No Content */
            204: {
                headers: {
                    [name: string]: unknown;
                };
                content?: never;
            };
        };
    };
```

And the matching request schemas:

```ts
        LoginRequest: {
            email: string;
            password: string;
        };
        ForgotPasswordRequest: {
            email: string;
        };
        ResetPasswordRequest: {
            token: string;
            newPassword: string;
        };
        UpdateUserRoleRequest: {
            role: "Viewer" | "Admin";
        };
```

Prefer running `npm run codegen` over the manual patch; the manual patch is the fallback when the backend is unavailable.

- [ ] **Step 8: Build + typecheck**

Run: `npm install && npm run build:test` in `frontend/`
Expected: passes (`vue-tsc` + `vite build`). Fix any type errors before committing.

- [ ] **Step 9: Commit**

```bash
git add frontend/src/ frontend/package.json frontend/package-lock.json
git commit -m "feat: rewrite frontend auth with local login and reset pages"
```

---

## Task 13: End-to-end verification + manual smoke checklist

**Files:** none (verification only)

- [ ] **Step 1: Backend build + tests**

Run in `backend/`: `dotnet build` and `dotnet test`
Expected: build succeeds, all tests pass.

- [ ] **Step 2: Frontend build**

Run in `frontend/`: `npm run build`
Expected: succeeds.

- [ ] **Step 3: Manual smoke test**

With the backend running (`dotnet run --project backend/Host`) and the frontend dev server (`npm run dev`):

1. Visit `/` while logged out → redirected to `/login`.
2. Log in with a valid user's credentials → lands on `/`, data loads.
3. Hard reload → still signed in (token persisted in localStorage).
4. Open `/login` while signed in → redirected to `/`.
5. Sign out (avatar menu / mobile drawer) → server session revoked; reloading shows `/login`.
6. On the login page, click "Forgot password?" → enter a known email → check the mail log/stub or SMTP inbox → click the link → set a new password → auto-signed in.
7. Create a user via `POST /users` with `role: "Viewer"` → confirm a setup email is sent → Viewer can read dashboards but cannot hit a write endpoint (403).
8. Change a user's role via `PATCH /users/{id}/role` → their sessions are revoked.
9. Delete a user → their sessions/reset tokens are gone (confirm via DB or next request is 401).

- [ ] **Step 4: Update `docs/superpowers/specs/2026-08-14-remove-auth0-auth-design.md` if implementation deviated from spec** (only if something changed).

- [ ] **Step 5: Commit any final fixes**

```bash
git add -A
git commit -m "chore: final verification of local session auth"
```

---

## Self-Review Notes

**Spec coverage:** All spec sections map to tasks — data model (T1–T3), session auth pipeline (T6), auth endpoints (T7), email/provisioning (T4–T5, T8), removal checklist (T9), frontend (T11–T12), tests (T10), smoke checklist (T13). Out-of-scope items (change-password UI, user-management page, idle timeout) are deliberately not implemented.

**Placeholder scan:** All code steps include complete code. The only tool-generated artifact is the EF migration (T3), which includes explicit verification steps.

**Type consistency:** `User.Create(name, email, role, companies)` is used consistently in CreateUserCommandHandler, AuthTests, and domain. `LoginResponse(token, UserResponse)` matches the `/auth/login` and `/auth/reset-password` responses and the frontend `storeSession(data.token, data.user)`. `UserResponse` includes `Role` throughout. Frontend `role` ref type is `"Viewer" | "Admin"` matching the generated schema.
