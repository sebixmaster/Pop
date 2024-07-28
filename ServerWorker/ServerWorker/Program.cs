using ServerWorker;

var host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((hostContext, services) =>
    {
    services.AddHostedService<Worker>();
})
    .Build();

host.Run();
