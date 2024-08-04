using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;
using System.Collections;

namespace SendMessageClient
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private string ServerIp;
        private int ServerPort;
        private List<string> incomingMessages = new List<string>();
        private TcpClient client;
        private static readonly Hashtable certificateErrors = new Hashtable();
        private X509Certificate2 clientCertificate;

        public struct Config
        {
            public string Server { get; set; }
            public int Port { get; set; }
        }

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            ReadConfig();
            LoadClientCertificate();
        }

        private void ReadConfig()
        {
            try
            {
                string text = File.ReadAllText(@"./config.json");
                var config = JsonSerializer.Deserialize<Config>(text);
                ServerIp = config.Server;
                ServerPort = config.Port;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading config file.");
                Environment.Exit(1);
            }
        }

        private static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            if (chain != null)
            {
                foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                {
                    Console.WriteLine($"Status: {chainStatus.Status}, {chainStatus.StatusInformation}");
                }
            }

            // Log detailed information about the certificate
            Console.WriteLine($"Certificate Subject: {certificate.Subject}");
            Console.WriteLine($"Certificate Issuer: {certificate.Issuer}");
            Console.WriteLine($"Certificate Effective Date: {certificate.GetEffectiveDateString()}");
            Console.WriteLine($"Certificate Expiration Date: {certificate.GetExpirationDateString()}");

            return false;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    client = new TcpClient();

                    await client.ConnectAsync(ServerIp, ServerPort);
                    _logger.LogInformation("Connected to the server.");

                    using (SslStream sslStream = new SslStream(
                        client.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate),
                        null
                    ))
                    {
                        try
                        {
                            sslStream.AuthenticateAsClient("", new X509CertificateCollection { clientCertificate }, SslProtocols.Tls12, checkCertificateRevocation: true);
                        }
                        catch (AuthenticationException e)
                        {
                            _logger.LogError(e, "Authentication failed - closing the connection.");
                            client.Close();
                            return;
                        }

                        byte[] buffer = new byte[2048];
                        while (!stoppingToken.IsCancellationRequested)
                        {
                            int bytes = await sslStream.ReadAsync(buffer, 0, buffer.Length, stoppingToken);

                            if (bytes > 0)
                            {
                                string message = Encoding.UTF8.GetString(buffer, 0, bytes);
                                incomingMessages.AddRange(message.Split(new[] { "<<EOF>>" }, StringSplitOptions.RemoveEmptyEntries));
                                WorkWithMessages(sslStream);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception occurred while connecting to the server.");
                }

                _logger.LogInformation("Reconnecting in 10 seconds...");
                await Task.Delay(10000, stoppingToken);
            }
        }

        private void WorkWithMessages(SslStream sslStream)
        {
            while (incomingMessages.Count > 0)
            {
                string msg = incomingMessages[0];
                incomingMessages.RemoveAt(0);

                using (var process = new Process())
                {
                    process.StartInfo.FileName = "C:\\PopUpBox.exe";
                    process.StartInfo.Arguments = $"\"{msg}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;

                    process.Start();
                    string response = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    SendConfirmationSecure(response, sslStream);
                }
            }
        }

        private async void SendConfirmationSecure(string responseMessage, SslStream stream)
        {
            try
            {
                byte[] response = Encoding.UTF8.GetBytes(responseMessage);
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending confirmation to the server.");
            }
        }

        private void LoadClientCertificate()
        {
            try
            {
                clientCertificate = new X509Certificate2("client.pfx", "1qaz@WSX");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading client certificate.");
                Environment.Exit(1);
            }
        }
    }
}
