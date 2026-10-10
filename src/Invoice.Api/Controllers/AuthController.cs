using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using Invoice.Api.Dtos.Auth;
using Invoice.Api.Services;
using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using DomainUser = Invoice.Domain.Entities.User;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للمصادقة — تسجيل دخول، تجديد، بيانات المستخدم، تغيير كلمة المرور.
/// </summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwt;
    private readonly IPasswordHasher _passwordHasher;
    private readonly JwtSettings _jwtSettings;

    public AuthController(
        AppDbContext db,
        IJwtService jwt,
        IPasswordHasher passwordHasher,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = db;
        _jwt = jwt;
        _passwordHasher = passwordHasher;
        _jwtSettings = jwtSettings.Value;
    }

    // ============ POST: /api/auth/login ============
    /// <summary>تسجيل الدخول — يرجع Access + Refresh + بيانات المستخدم.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await LoadUserWithRolesAndPermissionsAsync(u => u.Username == request.Username);

        // نستخدم نفس الرسالة لاسم المستخدم أو كلمة المرور الخطأ — لمنع تعداد المستخدمين
        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة." });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "الحساب معطّل. تواصل مع المدير." });
        }

        var (roles, permissions) = ExtractRolesAndPermissions(user);

        var accessToken = _jwt.GenerateAccessToken(user, roles, permissions);
        var refreshToken = _jwt.GenerateRefreshToken(user);

        // تحديث وقت آخر دخول
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
            User = BuildUserInfo(user, roles, permissions)
        });
    }

    // ============ POST: /api/auth/refresh ============
    /// <summary>تجديد الـ Access Token باستخدام Refresh Token صالح.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var principal = _jwt.ValidateToken(request.RefreshToken);
        if (principal == null)
        {
            return Unauthorized(new { message = "Refresh Token غير صالح أو منتهي." });
        }

        // التأكد من أن التوكن من نوع refresh (وليس access)
        var tokenType = principal.FindFirst("token_type")?.Value;
        if (tokenType != "refresh")
        {
            return Unauthorized(new { message = "نوع التوكن غير صالح للتجديد." });
        }

        var userId = GetUserIdFromPrincipal(principal);
        if (userId == null)
        {
            return Unauthorized(new { message = "التوكن لا يحتوي على معرّف مستخدم." });
        }

        var user = await LoadUserWithRolesAndPermissionsAsync(u => u.Id == userId.Value);
        if (user == null || !user.IsActive)
        {
            return Unauthorized(new { message = "الحساب غير موجود أو معطّل." });
        }

        var (roles, permissions) = ExtractRolesAndPermissions(user);

        var newAccessToken = _jwt.GenerateAccessToken(user, roles, permissions);
        var newRefreshToken = _jwt.GenerateRefreshToken(user);

        return Ok(new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenMinutes),
            User = BuildUserInfo(user, roles, permissions)
        });
    }

    // ============ GET: /api/auth/me ============
    /// <summary>بيانات المستخدم الحالي مع أدواره وصلاحياته.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoDto>> Me()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "لم يتم التعرف على المستخدم." });
        }

        var user = await LoadUserWithRolesAndPermissionsAsync(u => u.Id == userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "المستخدم غير موجود." });
        }

        var (roles, permissions) = ExtractRolesAndPermissions(user);
        return Ok(BuildUserInfo(user, roles, permissions));
    }

    // ============ POST: /api/auth/change-password ============
    /// <summary>تغيير كلمة مرور المستخدم الحالي.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "لم يتم التعرف على المستخدم." });
        }

        var user = await _db.Users.FindAsync(userId.Value);
        if (user == null)
        {
            return NotFound(new { message = "المستخدم غير موجود." });
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return BadRequest(new { message = "كلمة المرور الحالية غير صحيحة." });
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "تم تعديل هذا المستخدم بواسطة مستخدم آخر. أعد المحاولة." });
        }

        return NoContent();
    }

    // ============ Helpers ============

    /// <summary>تحميل المستخدم مع أدواره وصلاحياته (Eager Loading).</summary>
    private async Task<DomainUser?> LoadUserWithRolesAndPermissionsAsync(
        Expression<Func<DomainUser, bool>> predicate)
    {
        return await _db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role!)
                    .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(predicate);
    }

    /// <summary>استخراج أسماء الأدوار وأكواد الصلاحيات من كيانات الربط.</summary>
    private static (List<string> Roles, List<string> Permissions) ExtractRolesAndPermissions(DomainUser user)
    {
        var roles = user.UserRoles
            .Where(ur => ur.Role != null)
            .Select(ur => ur.Role!.Name)
            .Distinct()
            .ToList();

        var permissions = user.UserRoles
            .Where(ur => ur.Role != null)
            .SelectMany(ur => ur.Role!.RolePermissions)
            .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.Code)
            .Distinct()
            .ToList();

        return (roles, permissions);
    }

    /// <summary>بناء UserInfoDto من الكيان + قوائم الأدوار/الصلاحيات.</summary>
    private static UserInfoDto BuildUserInfo(DomainUser user, List<string> roles, List<string> permissions)
    {
        return new UserInfoDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Roles = roles,
            Permissions = permissions
        };
    }

    /// <summary>قراءة معرّف المستخدم من Claims الـ Request الحالي.</summary>
    private int? GetCurrentUserId()
    {
        var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return int.TryParse(value, out var id) ? id : null;
    }

    /// <summary>قراءة معرّف المستخدم من ClaimsPrincipal مُمرَّر.</summary>
    private static int? GetUserIdFromPrincipal(ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return int.TryParse(value, out var id) ? id : null;
    }
}