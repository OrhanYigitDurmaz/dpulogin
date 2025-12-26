//using dpulogin;
//var builder = Host.CreateApplicationBuilder(args);
//builder.Services.AddHostedService<Worker>();
//var host = builder.Build();
//host.Run();

using dpulogin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging.EventLog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// Register as a Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "DPU Auto Login Service";
});

// Enable Event Log logging (required for Windows Services)
LoggerProviderOptions.RegisterProviderOptions<EventLogSettings, EventLogLoggerProvider>(builder.Services);

// Register worker
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();

