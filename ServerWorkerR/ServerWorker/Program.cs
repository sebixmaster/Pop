using Microsoft.VisualBasic;
using ServerWorker;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<Worker>();
    })
    .ConfigureAppConfiguration((hostContext, config) =>
    {
        config
            .SetBasePath("")
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
    })
    .Build();
host.Run();