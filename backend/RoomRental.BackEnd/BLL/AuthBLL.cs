using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RoomRental.BackEnd.DTO.Auth;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.Models;
using RoomRental.BackEnd.DAL;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace RoomRental.BackEnd.BLL;

/// <summary>
/// Service xử lý Authentication
/// </summary>
public class AuthBLL : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthBLL(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        var fullName = registerDto.GetFullName();
        var email = registerDto.Email?.Trim();
        var phone = registerDto.GetPhone();
        var password = registerDto.GetPassword();
        var confirmPassword = registerDto.GetConfirmPassword();
        var roleId = ResolveRegisterRole(registerDto.GetRoleName());

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new Exception("Họ tên là bắt buộc");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new Exception("Email là bắt buộc");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new Exception("Số điện thoại là bắt buộc");
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            throw new Exception("Mật khẩu phải có ít nhất 6 ký tự");
        }

        if (password != confirmPassword)
        {
            throw new Exception("Mật khẩu xác nhận không khớp");
        }

        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email || u.Phone == phone || u.UserName == email);

        if (existingUser != null)
        {
            throw new Exception("Email hoặc số điện thoại đã được sử dụng");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            UserName = email,
            FullName = fullName,
            Email = email,
            Phone = phone,
            PasswordHash = passwordHash,
            RoleId = roleId,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        if (roleId == 1)
        {
            _context.TenantProfiles.Add(new TenantProfile { AccountId = user.Id });
        }
        else if (roleId == 2)
        {
            _context.LandlordProfiles.Add(new LandlordProfile { AccountId = user.Id });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.RoleName,
            AvatarUrl = user.AvatarUrl
        };
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        var email = loginDto.GetEmail();
        var password = loginDto.GetPassword();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new Exception("Email và mật khẩu là bắt buộc");
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        if (user == null)
        {
            throw new Exception("Email hoặc mật khẩu không đúng");
        }

        // Kiểm tra tài khoản có bị khóa không
        if (user.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin");
        }

        var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

        if (!isPasswordValid)
        {
            throw new Exception("Email hoặc mật khẩu không đúng");
        }

        // Tạo JWT Token
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Role = user.RoleName,
            AvatarUrl = user.AvatarUrl
        };
    }

    /// <summary>
    /// Tạo JWT Token
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new Exception("JWT SecretKey không được cấu hình");
        var issuer = jwtSettings["Issuer"] ?? "RoomRentalAPI";
        var audience = jwtSettings["Audience"] ?? "RoomRentalClient";
        var expiryInMinutes = int.Parse(jwtSettings["ExpiryInMinutes"] ?? "1440"); // Default 24h

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.RoleName),
            new Claim("UserId", user.Id.ToString()),
            new Claim("RoleId", user.RoleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryInMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static int ResolveRegisterRole(string roleName)
    {
        var normalized = roleName.Trim().ToLowerInvariant();

        return normalized switch
        {
            "tenant" or "nguoidung" or "người dùng" or "user" or "1" => 1,
            "landlord" or "chutro" or "chủ trọ" or "2" => 2,
            "admin" or "0" => throw new Exception("Không thể đăng ký với quyền Admin"),
            _ => throw new Exception("Role không hợp lệ. Chỉ hỗ trợ Tenant hoặc Landlord")
        };
    }
}
