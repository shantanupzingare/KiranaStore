using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace KiranaStore.Infrastructure.Logging;

public static class SerilogConfig
{
    public static void Configure(IConfiguration config)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/kirana-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
            .CreateLogger();
    }
}
