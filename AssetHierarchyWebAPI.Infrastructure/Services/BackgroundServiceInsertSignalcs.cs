using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Domain.Entities;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using AssetHierarchyWebAPI.Infrastructure.Services;
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
                var signalValues = new List<SignalValue>();


                    signalValues.Add(new SignalValue
                    {
                        SignalValueData = random.NextDouble() * 1000,
                        SignalId = random.Next(1, signalCount+1),
                        RecordedAt = DateTime.UtcNow
                    });
                

                await dbContext.SignalValues.AddRangeAsync(signalValues, stoppingToken);

                await dbContext.SaveChangesAsync(stoppingToken);


                await _notificationService.SendAsync(
                    $""
                );
            }
            catch (Exception ex)
            {
               
            }

            await Task.Delay(2000, stoppingToken); 
        }
    }
}
