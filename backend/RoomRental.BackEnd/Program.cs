using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RoomRental.BackEnd.BLL;
using RoomRental.BackEnd.BLL.Interfaces;
using RoomRental.BackEnd.DAL;
using RoomRental.BackEnd.Hubs;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Đăng ký DbContext với SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Đăng ký toàn bộ BLL Services
builder.Services.AddScoped<IAuthService, AuthBLL>();
builder.Services.AddScoped<IUserService, UserBLL>();
builder.Services.AddScoped<IPostService, PostBLL>();
builder.Services.AddScoped<IFavoriteService, FavoriteBLL>();
builder.Services.AddScoped<IViewingAppointmentService, ViewingAppointmentBLL>();
builder.Services.AddScoped<IRoomService, RoomBLL>();
builder.Services.AddScoped<ICategoryService, CategoryBLL>();
builder.Services.AddScoped<IAmenityService, AmenityBLL>();
builder.Services.AddScoped<IReportService, ReportBLL>();
builder.Services.AddScoped<IBlogService, BlogBLL>();
builder.Services.AddScoped<IAdminService, AdminBLL>();
builder.Services.AddScoped<IRentalRequestService, RentalRequestBLL>();
builder.Services.AddScoped<IDepositService, DepositBLL>();
builder.Services.AddScoped<IRentalContractService, RentalContractBLL>();
builder.Services.AddScoped<IIncidentService, IncidentBLL>();
builder.Services.AddScoped<IRoomReviewService, RoomReviewBLL>();

// Đăng ký Nhóm tính năng mới: Chat, Thông báo, Thanh toán VNPay
builder.Services.AddScoped<IChatService, ChatBLL>();
builder.Services.AddScoped<INotificationService, NotificationBLL>();
builder.Services.AddScoped<IPaymentService, PaymentBLL>();

// Thêm SignalR hỗ trợ Real-time Chat & Notifications
builder.Services.AddSignalR();

// Cấu hình JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "RoomRentalSecretKeyForJwtAuthentication2026123456";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "RoomRentalAPI",
        ValidAudience = jwtSettings["Audience"] ?? "RoomRentalClient",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    // Hỗ trợ truyền JWT Token qua query string cho SignalR WebSockets
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/hubs/chat") || path.StartsWithSegments("/hubs/notifications")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Add Authorization
builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Cấu hình Swagger với JWT Bearer Authentication & Tránh xung đột Schema
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Room Rental API - Hệ Thống Tìm Kiếm & Cho Thuê Phòng Trọ",
        Version = "v1",
        Description = "API cho 3 Roles: Tenant (Người thuê), Landlord (Chủ trọ), Admin (Quản trị viên)"
    });

    // Tránh lỗi xung đột Schema ID
    c.CustomSchemaIds(type => type.FullName);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập JWT Bearer token theo định dạng: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            new List<string>()
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5173",
                  "http://localhost:5174",
                  "http://localhost:5175",
                  "http://localhost:3000",
                  "http://127.0.0.1:5173",
                  "http://127.0.0.1:5174",
                  "http://127.0.0.1:5175",
                  "http://127.0.0.1:3000"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Seed dữ liệu mặc định
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedAdminAsync(context);
        await DbInitializer.SeedCategoriesAsync(context);
        await DbInitializer.SeedAmenitiesAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi seed dữ liệu");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Room Rental API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowFrontend");

// Use Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
