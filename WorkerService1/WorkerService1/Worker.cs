using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WorkerService1
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private const string ServerIp = "127.0.0.1"; // Update with your server's IP address
        private const int ServerPort = 8888;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (TcpClient client = new TcpClient())
                    {
                        await client.ConnectAsync(ServerIp, ServerPort);
                        _logger.LogInformation("Connected to the server.");

                        NetworkStream stream = client.GetStream();
                        byte[] buffer = new byte[1024];

                        while (!stoppingToken.IsCancellationRequested)
                        {
                            try
                            {
                                if (stream.DataAvailable)
                                {
                                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);
                                    if (bytesRead == 0)
                                    {
                                        _logger.LogInformation("Server closed the connection.");
                                        break;
                                    }

                                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                                    string[] messageParts = message.Split("<<<>>>");
                                    string messageType = messageParts[0];


                                    if (messageType == "0x021" || messageType != "0x007")
                                    {
                                        break;
                                    }

                                    // Handle the message as needed
                                    ShowMessage(message);
                                }
                            }
                            catch (IOException ex)
                            {
                                _logger.LogError(ex, "NetworkStream error. Assuming server closed the connection.");
                                break;
                            }

                            await Task.Delay(500); // Adjust delay as needed
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while connecting to the server.");
                }

                _logger.LogInformation("Reconnecting in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private void ShowMessage(string message)
        {
            string[] messageParts = message.Split("<<<>>>");
            string messageSender = messageParts[1];
            string messageTopic = messageParts[2];
            string messageContent = messageParts[3];
            // Implement the logic to show the message in a popup or any other form of notification
            Console.WriteLine($"Popup Message: from {messageSender} Topic: {messageTopic} Message: {message}");

            using (var process = new Process())
            {
                process.StartInfo.FileName = "C:\\Users\\Sebastian\\Desktop\\POP\\PopUp\\PopUp\\bin\\Debug\\PopUp.exe";
                process.StartInfo.Arguments = $"\"{message}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;

                process.Start();
                string response = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine(response);
            }

        }
    }
}
