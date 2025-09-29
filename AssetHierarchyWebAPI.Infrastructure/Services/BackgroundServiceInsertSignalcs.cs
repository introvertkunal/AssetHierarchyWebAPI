using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class BackgroundServiceInsertSignal : BackgroundService
{
    private readonly IServiceProvider _service;
    private readonly INotificationService _notificationService;

    public BackgroundServiceInsertSignal(IServiceProvider service, INotificationService notificationService)
    {
        _service = service;
        _notificationService = notificationService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _service.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AssetContext>();

                int signalCount = await dbContext.AssetSignal.CountAsync(stoppingToken);

                var random = new Random();
                var signalValues = new SignalValue();

                signalValues.SignalValueData = random.NextDouble() * 1000;
                signalValues.SignalId = random.Next(1, signalCount + 1);
                signalValues.RecordedAt = DateTime.UtcNow;
                   
              
                await dbContext.SignalValues.AddRangeAsync(signalValues);

                await dbContext.SaveChangesAsync(stoppingToken);

                var signal = await dbContext.AssetSignal
                            .Include(s => s.AssetNode)
                            .FirstOrDefaultAsync(s => s.SignalId == signalValues.SignalId, stoppingToken);


                await _notificationService.SendAsync(
                    $"{signalValues.SignalValueData} value is added to Signal {signal.SignalName} under Asset {signal.AssetNode?.Name}"
                );
            }
            catch (Exception ex)
            {
                await _notificationService.SendAsync(
                    $"Failed to add Signal value to Signal"
                );
            }

            await Task.Delay(10000, stoppingToken); 
        }
    }
}
