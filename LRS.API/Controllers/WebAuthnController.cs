using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Fido2NetLib;
using Fido2NetLib.Objects;
using LRS.Domain.Entities;
using LRS.Infrastructure.Persistence;

namespace LRS.API.Controllers;

[ApiController]
[Route("api/auth/webauthn")]
public class WebAuthnController : ControllerBase
{
    private readonly IFido2 _fido2;
    private readonly LrsDbContext _dbContext;
    private readonly IConfiguration _config;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan OptionsTtl = TimeSpan.FromMinutes(5);

    public WebAuthnController(
        IFido2 fido2,
        LrsDbContext dbContext,
        IConfiguration config,
        IMemoryCache cache)
    {
        _fido2 = fido2;
        _dbContext = dbContext;
        _config = config;
        _cache = cache;
    }

    [HttpPost("registerOptions")]
    [AllowAnonymous]
    public async Task<IActionResult> MakeCredentialOptions([FromBody] WebAuthnRegisterOptionsRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Username is required." });

        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Password is required to register a passkey." });

        var userEntity = await _dbContext.Users
            .Include(u => u.FidoCredentials)
            .FirstOrDefaultAsync(u => u.Email == username);

        if (userEntity == null)
            return BadRequest(new { message = "User not found" });

        if (!BCrypt.Net.BCrypt.Verify(request.Password, userEntity.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password" });

        var user = new Fido2User
        {
            DisplayName = username,
            Name = username,
            Id = Encoding.UTF8.GetBytes(username)
        };

        var existingKeys = userEntity.FidoCredentials
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToList();

        // Prefer this device's platform authenticator (Windows Hello, Touch ID, etc.) so
        // discoverable / fingerprint sign-in works on the same machine.
        var authenticatorSelection = new AuthenticatorSelection
        {
            AuthenticatorAttachment = AuthenticatorAttachment.Platform,
            ResidentKey = ResidentKeyRequirement.Required,
            UserVerification = UserVerificationRequirement.Required
        };

        var options = _fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = user,
            ExcludeCredentials = existingKeys,
            AuthenticatorSelection = authenticatorSelection,
            AttestationPreference = AttestationConveyancePreference.None,
            Extensions = null
        });

        _cache.Set(RegisterCacheKey(username), options, OptionsTtl);

        return Ok(options);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> MakeCredential([FromBody] WebAuthnRegisterRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest(new { message = "Username is required." });

        if (!_cache.TryGetValue(RegisterCacheKey(username), out CredentialCreateOptions? options) || options == null)
            return BadRequest(new { message = "Registration options not found. Please start over." });

        _cache.Remove(RegisterCacheKey(username));

        var userEntity = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == username);
        if (userEntity == null)
            return BadRequest(new { message = "User not found" });

        IsCredentialIdUniqueToUserAsyncDelegate callback = async (args, cancellationToken) =>
        {
            var credentials = await _dbContext.FidoCredentials.ToListAsync(cancellationToken);
            return !credentials.Any(c => c.CredentialId.SequenceEqual(args.CredentialId));
        };

        try
        {
            var success = await _fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = request.Response,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = callback
            }, cancellationToken: default);

            var cred = new FidoCredential
            {
                AppUserId = userEntity.Id,
                CredentialId = success.Id,
                PublicKey = success.PublicKey,
                UserHandle = success.User.Id,
                SignatureCounter = success.SignCount,
                RegDate = DateTime.UtcNow,
                AaGuid = success.AaGuid,
                CredType = 1
            };

            _dbContext.FidoCredentials.Add(cred);
            await _dbContext.SaveChangesAsync();

            return Ok(new { status = "ok", errorMessage = "" });
        }
        catch (Exception e)
        {
            return BadRequest(new { status = "error", errorMessage = e.Message });
        }
    }

    [HttpPost("loginOptions")]
    [AllowAnonymous]
    public async Task<IActionResult> AssertionOptions([FromBody] WebAuthnLoginRequest request)
    {
        var username = request.Username?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            var discoverableOptions = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
            {
                UserVerification = UserVerificationRequirement.Required,
                Extensions = null
            });

            _cache.Set(DiscoverableLoginCacheKey, discoverableOptions, OptionsTtl);
            return Ok(discoverableOptions);
        }

        var existingCredentials = await _dbContext.FidoCredentials
            .Include(c => c.AppUser)
            .Where(c => c.AppUser != null && c.AppUser.Email == username)
            .Select(c => new PublicKeyCredentialDescriptor(c.CredentialId))
            .ToListAsync();

        if (!existingCredentials.Any())
            return BadRequest(new { message = "No credentials found for user." });

        var options = _fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = existingCredentials,
            UserVerification = UserVerificationRequirement.Required,
            Extensions = null
        });

        _cache.Set(LoginCacheKey(username), options, OptionsTtl);

        return Ok(options);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> MakeAssertion([FromBody] WebAuthnAssertionRequest request)
    {
        var credId = request.Response.RawId;

        var allCredentials = await _dbContext.FidoCredentials
            .Include(c => c.AppUser)
            .ToListAsync();

        var credential = allCredentials.FirstOrDefault(c => c.CredentialId.SequenceEqual(credId));

        if (credential == null || credential.AppUser == null)
            return BadRequest(new { message = "Unknown credential" });

        var requestedUsername = request.Username?.Trim() ?? string.Empty;
        var cacheKey = string.IsNullOrWhiteSpace(requestedUsername)
            ? DiscoverableLoginCacheKey
            : LoginCacheKey(requestedUsername);

        if (!_cache.TryGetValue(cacheKey, out AssertionOptions? options) || options == null)
            return BadRequest(new { message = "Login options not found." });

        _cache.Remove(cacheKey);

        if (!string.IsNullOrWhiteSpace(requestedUsername) &&
            !string.Equals(credential.AppUser.Email, requestedUsername, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Credential does not belong to this user." });
        }

        IsUserHandleOwnerOfCredentialIdAsync callback = (args, token) =>
        {
            if (args.UserHandle == null || args.UserHandle.Length == 0)
            {
                return Task.FromResult(true);
            }

            return Task.FromResult(credential.UserHandle.AsSpan().SequenceEqual(args.UserHandle));
        };

        try
        {
            var res = await _fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = request.Response,
                OriginalOptions = options,
                StoredPublicKey = credential.PublicKey,
                StoredSignatureCounter = credential.SignatureCounter,
                IsUserHandleOwnerOfCredentialIdCallback = callback
            }, cancellationToken: default);

            credential.SignatureCounter = res.SignCount;
            await _dbContext.SaveChangesAsync();

            var token = GenerateJwt(credential.AppUser.Email, credential.AppUser.Role);

            return Ok(new LoginResponse
            {
                Token = token,
                Username = credential.AppUser.Email,
                ExpiresAt = GetTokenExpiryUtc()
            });
        }
        catch (Exception e)
        {
            return BadRequest(new { message = e.Message });
        }
    }

    private DateTime GetTokenExpiryUtc()
    {
        var durationMinutes = GetJwtDurationMinutes();
        return DateTime.UtcNow.AddMinutes(durationMinutes);
    }

    private int GetJwtDurationMinutes() =>
        int.TryParse(_config["Jwt:DurationInMinutes"], out var m) ? m : 480;

    private static string RegisterCacheKey(string username) =>
        $"webauthn:register:{username.ToLowerInvariant()}";

    private static string LoginCacheKey(string username) =>
        $"webauthn:login:{username.ToLowerInvariant()}";

    private const string DiscoverableLoginCacheKey = "webauthn:login:discoverable";

    private string GenerateJwt(string username, string role)
    {
        var jwtSettings = _config.GetSection("Jwt");
        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key is missing"));
        var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        var issuer = jwtSettings["Issuer"] ?? "LRS.API";
        var audience = jwtSettings["Audience"] ?? "LRS.Client";
        var durationMinutes = GetJwtDurationMinutes();

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("role", role)
        };

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(durationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class WebAuthnRegisterOptionsRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class WebAuthnLoginRequest
{
    public string Username { get; set; } = string.Empty;
}

public class WebAuthnRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public AuthenticatorAttestationRawResponse Response { get; set; } = null!;
}

public class WebAuthnAssertionRequest
{
    public string Username { get; set; } = string.Empty;
    public AuthenticatorAssertionRawResponse Response { get; set; } = null!;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
