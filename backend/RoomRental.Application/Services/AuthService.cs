using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RoomRental.Application.DTOs.Auth;
using RoomRental.Application.Interfaces;
using RoomRental.Domain.Entities;
using RoomRental.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace RoomRental.Application.Services;

/// <summary>
/// Service xử lý Authentication
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    /// <summary>
    /// Đăng ký tài khoản mới
    /// </summary>
    public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
    {
        // Kiểm tra email đã tồn tại chưa
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

        if (existingUser != null)
        {
            throw new Exception("Email đã được sử dụng");
        }

        // Lấy Role từ RoleName
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == registerDto.RoleName);

        if (role == null)
        {
            throw new Exception("Role không hợp lệ");
        }

        // Không cho phép đăng ký với role Admin
        if (role.Name == "Admin")
        {
            throw new Exception("Không thể đăng ký với quyền Admin");
        }

        // Hash password bằng BCrypt
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        // Tạo User mới
        var user = new User
        {
            FullName = registerDto.FullName,
            Email = registerDto.Email,
            Phone = registerDto.Phone,
            PasswordHash = passwordHash,
            RoleId = role.Id,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Load lại để có thông tin Role
        await _context.Entry(user).Reference(u => u.Role).LoadAsync();

        // Tạo JWT Token
        var token = GenerateJwtToken(user);

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.Name,
            AvatarUrl = user.AvatarUrl
        };
    }

    /// <summary>
    /// Đăng nhập
    /// </summary>
    public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
    {
        // Tìm User theo Email
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
        {
            throw new Exception("Email hoặc mật khẩu không đúng");
        }

        // Kiểm tra tài khoản có bị khóa không
        if (user.IsBlocked)
        {
            throw new Exception("Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin");
        }

        // Verify password bằng BCrypt
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);

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
            Email = user.Email,
            Role = user.Role.Name,
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
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.Role.Name),
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
}
