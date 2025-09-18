using AssetHierarchyWebAPI.API.Hubs;
using AssetHierarchyWebAPI.Application.Interfaces;
using AssetHierarchyWebAPI.Infrastructure.Persistence;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetHierarchyWebAPI.Infrastructure.Services
{
    public class BackgroundSignalValueAverage : BackgroundService
    {
        private readonly IManagerQueue _queue;
        private readonly IServiceProvider _service;
        private readonly INotificationService _notificationservice;

        public BackgroundSignalValueAverage(IManagerQueue queue, IServiceProvider service, INotificationService notificationService )
        {
            _queue = queue;
            _service = service;
            _notificationservice = notificationService;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_queue.TryDequeue(out int id))
                    {
                        using var scope = _service.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AssetContext>();

                        var signal = await dbContext.AssetSignal
                            .Include(s => s.AssetNode)
                            .FirstOrDefaultAsync(s => s.SignalId == id, stoppingToken);

                        if (signal != null)
                        {
                            var hasValues = await dbContext.SignalValues.AnyAsync(sv => sv.SignalId == id, stoppingToken);

                            if (hasValues)
                            {
                                var average = await dbContext.SignalValues
                                    .Where(sv => sv.SignalId == id)
                                    .AverageAsync(sv => sv.SignalValueData, stoppingToken);

                                await _notificationservice.SendAsync(
                                    $"The Average Value of {signal.SignalName} under Asset {signal.AssetNode?.Name} is {average:F2}"
                                );

                                Console.WriteLine($"The Average Value of {signal.SignalName} under Asset {signal.AssetNode?.Name} is {average:F2}");
                            }
                            else
                            {
                                await _notificationservice.SendAsync(
                                    $"No Signal Values found for {signal.SignalName} under Asset {signal.AssetNode?.Name} to calculate average."
                                );
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await _notificationservice.SendAsync($"Error: {ex.Message}");
                }

                await Task.Delay(500, stoppingToken);
            }
        }

    }
}
