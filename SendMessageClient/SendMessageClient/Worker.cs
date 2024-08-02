using SendMessageClient;
using System;
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
        public string ServerIp;
        public Int32 ServerPort;
        public List<string> incomingMessages = new List<string>();
        private TcpClient client;
        private static Hashtable certificateErrors = new Hashtable();

        public struct Config
        {
            public string Server { get; set; }
            public int Port { get; set; }
        }



        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            ReadConfig();
        }

        public void ReadConfig()
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
                Console.WriteLine(ex.Message);
                System.Environment.Exit(1);
            }
        }

        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

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

                    SslStream sslStream = new SslStream(
                        client.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate),
                        null
                    );

                    try
                    {
                        sslStream.AuthenticateAsClient("SelfSignedCert");
                    }
                    catch (AuthenticationException e)
                    {
                        Console.WriteLine("Exception: {0}", e.Message);
                        if (e.InnerException != null)
                        {
                            Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                        }
                        Console.WriteLine("Authentication failed - closing the connection.");
                        client.Close();
                        return;
                    }

                    byte[] buffer = new byte[2048];
                    while (!stoppingToken.IsCancellationRequested)
                    {                       
                        int bytes = sslStream.Read(buffer, 0, buffer.Length);

                        string message = Encoding.UTF8.GetString(buffer, 0 ,bytes);
                        string[] messageParts = message.Split("<<<>>>");
                        string messageType = messageParts[0];

                        if (messageType == "0x021" || messageType != "0x007")
                        {
                            break;
                        }

                        if (message.Contains("<<EOF>>"))
                        {
                            string[] separatedMessages = message.Split("<<EOF>>");
                            foreach (string m in separatedMessages)
                            {
                                if (m.Length > 0)
                                {
                                    incomingMessages.Add(m);
                                }
                            }
                        }
                        WorkWithMessages(sslStream);
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

        static string ReadMessage(SslStream sslStream)
        {

            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);

                if (messageData.ToString().IndexOf("<<EOF>>") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }

        private void WorkWithMessages(SslStream stream)
        {
            while (incomingMessages.Count > 0)
            {
                string msg = incomingMessages[0];
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "C:\\PopUpBox.exe";
                    process.StartInfo.Arguments = $"\"{msg}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;

                    process.Start();
                    string response = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    SendConfirmationSecure(response, stream);
                }
                incomingMessages.RemoveAt(0);
            }
        }

        private async void SendConfirmation(string repsonseMessage, NetworkStream stream)
        {
            try
            {
                byte[] response = Encoding.UTF8.GetBytes(repsonseMessage);
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async void SendConfirmationSecure(string repsonseMessage, SslStream stream)
        {
            try
            {
                byte[] response = Encoding.UTF8.GetBytes(repsonseMessage);
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
