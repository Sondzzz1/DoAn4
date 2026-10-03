using RoomRental.BackEnd.BLL.Interfaces;

namespace RoomRental.BackEnd.Services;

public sealed class RentalLifecycleHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RentalLifecycleHostedService> _logger;

    public RentalLifecycleHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<RentalLifecycleHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var enabled = _configuration.GetValue<bool>("RentalLifecycle:Enabled");
            var intervalMinutes = Math.Clamp(_configuration.GetValue<int?>("RentalLifecycle:IntervalMinutes") ?? 60, 5, 24 * 60);

            if (enabled)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var contractService = scope.ServiceProvider.GetRequiredService<IRentalContractService>();
                    var pendingSignatureExpiryHours = _configuration.GetValue<int?>("RentalLifecycle:PendingSignatureExpiryHours") ?? 72;
                    await contractService.ReconcileContractLifecycleAsync(pendingSignatureExpiryHours, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Không thể đồng bộ lifecycle hợp đồng thuê.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
