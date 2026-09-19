using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient("RoomRentalApi", client =>
{
    var backendBaseUrl = builder.Configuration["Backend:BaseUrl"] ?? "http://localhost:5000";
    client.BaseAddress = new Uri(backendBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "RoomRental Gateway",
    backend = builder.Configuration["Backend:BaseUrl"] ?? "http://localhost:5000"
}));

app.MapMethods("/api/{**path}", [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Delete, HttpMethods.Patch, HttpMethods.Options], async context =>
{
    if (context.Request.Method == HttpMethods.Options)
    {
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        return;
    }

    var backendBaseUrl = builder.Configuration["Backend:BaseUrl"] ?? "http://localhost:5000";
    var pathValue = context.Request.Path.Value ?? "/";
    var targetPath = pathValue.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
        ? pathValue
        : $"/api{pathValue}";

    var targetUrl = $"{backendBaseUrl.TrimEnd('/')}{targetPath}{context.Request.QueryString.Value ?? string.Empty}";

    using var requestMessage = new HttpRequestMessage(new HttpMethod(context.Request.Method), targetUrl);

    if (context.Request.ContentLength > 0 || context.Request.Body.CanRead)
    {
        context.Request.EnableBuffering();

        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
        }

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var requestBody = await reader.ReadToEndAsync();

        if (!string.IsNullOrEmpty(requestBody))
        {
            requestMessage.Content = new StringContent(requestBody, Encoding.UTF8);

            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
            {
                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(context.Request.ContentType);
            }
        }

        if (context.Request.Body.CanSeek)
        {
            context.Request.Body.Position = 0;
        }
    }

    foreach (var header in context.Request.Headers)
    {
        if (header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
            header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && requestMessage.Content is not null)
        {
            continue;
        }

        requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    }

    var httpClientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
    var client = httpClientFactory.CreateClient("RoomRentalApi");

    try
    {
        using var backendResponse = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

        context.Response.StatusCode = (int)backendResponse.StatusCode;

        foreach (var header in backendResponse.Headers)
        {
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = header.Value.FirstOrDefault() ?? "application/json";
                continue;
            }

            if (!context.Response.Headers.ContainsKey(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in backendResponse.Content.Headers)
        {
            if (header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Connection", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = header.Value.FirstOrDefault() ?? "application/json";
                continue;
            }

            if (!context.Response.Headers.ContainsKey(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        var payload = await backendResponse.Content.ReadAsByteArrayAsync();
        if (payload.Length > 0)
        {
            await context.Response.Body.WriteAsync(payload);
        }
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = StatusCodes.Status502BadGateway;
        await context.Response.WriteAsJsonAsync(new
        {
            success = false,
            message = "Gateway không thể kết nối tới backend.",
            error = ex.Message
        });
    }
});

app.Run();
