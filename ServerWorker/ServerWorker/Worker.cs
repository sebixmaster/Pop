using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace ServerWorker
{
    public class Worker : BackgroundService
    {
        private const int Port = 8888;
        private static readonly IPAddress LocalAddr = IPAddress.Parse("127.0.0.1");
        private static readonly ConcurrentDictionary<string, TcpClient> Clients = new ConcurrentDictionary<string, TcpClient>();
        private Timer _timer;
        private TcpListener _server;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _server = new TcpListener(LocalAddr, Port);
            _server.Start();

            _logger.LogInformation("Server started. Waiting for connections...");
            _timer = new Timer(CheckDatabase, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            stoppingToken.Register(() =>
            {
                _logger.LogInformation("Shutting down server...");
                ShutdownServerAsync();
                _server.Stop();
                _timer?.Dispose();
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_server.Pending())
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }

                await Task.Delay(1000, stoppingToken); // Small delay to prevent tight loop
            }
        }

        private async Task ShutdownServerAsync()
        {
            foreach (var client in Clients.Values)
            {
                try
                {
                    await SendMessageToClient(client, "0x021");
                    client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception closing client connection: {ex.Message}");
                }
            }

            Clients.Clear();
            Console.WriteLine("Server has shut down.");
        }

        private async void CheckDatabase(object state)
        {
            foreach (var client in Clients)
            {
                _logger.LogInformation($"Checking database for {client.Key}");
                await SendMessageToClient(client.Value, "0x007", "Checking database...", "seba", "U£A");
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            string clientIp = client.Client.RemoteEndPoint.ToString();
            Clients.TryAdd(clientIp, client);
            _logger.LogInformation($"Client connected: {clientIp}");
        }

        private async Task SendMessageToClient(TcpClient client, string messageCode, string? topic = "", string? person = "", string? message = "")
        {
            try
            {
                NetworkStream stream = client.GetStream();
                string formatMessage = $"{messageCode}<<<>>>{person}<<<>>>{topic}<<<>>>{message}";
                byte[] response = Encoding.UTF8.GetBytes(formatMessage);
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception sending message to {client.Client.RemoteEndPoint}: {ex.Message}");
                if (client.Client.RemoteEndPoint != null)
                {
                    string clientIp = client.Client.RemoteEndPoint.ToString();
                    if (Clients.TryRemove(clientIp, out TcpClient _))
                    {
                        client.Close();
                        _logger.LogInformation($"Client removed due to error: {clientIp}");
                    }
                }
            }
        }
    }
}
