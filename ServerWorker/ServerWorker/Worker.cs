using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Net.Security;
using System;

namespace ServerWorker
{
    public class Worker : BackgroundService
    {
        // Database
        public string connString = String.Empty;
        public Socket serverSocket;
        public SqlConnection con;
        public List<SendToValue> pcToSend = new List<SendToValue>();

        // Server setup
        private int Port { get; set; }
        private IPAddress LocalAddr { get; set; }
        private static readonly ConcurrentDictionary<string, TcpClient> Clients = new ConcurrentDictionary<string, TcpClient>();
        private TcpListener _server;

        // Others
        private Timer _timer;
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private X509Certificate serverCertificate;
        private string certificate;


        public struct SendToValue
        {
            public int iD;
            public string pcName;
            public string topic;
            public string message;
            public string createUser;
        }


        public Worker(ILogger<Worker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Setup Data
            Config workerConfig = _configuration.GetSection("Config").Get<Config>();
            LocalAddr = IPAddress.Parse(workerConfig.Ip);
            Port = workerConfig.Port;
            connString = workerConfig.ConnectionString;
            certificate = workerConfig.Certificate;

            // Setup database
            con = new SqlConnection(connString);

            // Setup server
            serverCertificate = new X509Certificate("C:\\Users\\olekk\\Desktop\\server.pfx", "1qaz@WSX");

            _server = new TcpListener(LocalAddr, Port);
            _server.Start();
            _logger.LogInformation("Server started. Waiting for connections...");
            

            _timer = new Timer(CheckDatabase, null, TimeSpan.Zero, TimeSpan.FromSeconds(15));
                       
            stoppingToken.Register(() =>
            {
               // ShutdownServerAsync();
                _logger.LogInformation("Shutting down server...");
                _server.Stop();
                _timer?.Dispose();
                con.Close();
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_server.Pending())
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
                await Task.Delay(500, stoppingToken); // Small delay to prevent tight loop
            }
        }


        public void GetValuesFromDatabase()
        {
            try
            {
                pcToSend.Clear();
                con.Open();
                string query = "SELECT * FROM base WHERE readdate is NULL OR [read] = 0";
                SqlCommand cmd = new(query, con);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    SendToValue set;
                    set.iD = reader.GetInt32(0);
                    set.pcName = reader.GetString(2);
                    set.topic = reader.GetString(3);
                    set.message = reader.GetString(4);
                    set.createUser = reader.GetString(9);

                    pcToSend.Add(set);
                }
                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }


        private async void CheckDatabase(object state)
        {
            GetValuesFromDatabase();
            if (Clients.Count > 0 && pcToSend.ToArray().Length > 0)
            {
                string firstIp;
                foreach (SendToValue val in pcToSend.ToArray())
                {
                    firstIp = String.Empty;
                    try
                    {
                        IPAddress ip = Dns.GetHostEntry(val.pcName.Trim()).AddressList.First(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                        IPAddress[] addresslist = Dns.GetHostAddresses(val.pcName.Trim());
                        firstIp = addresslist[1].ToString();    
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                    if (firstIp != String.Empty)
                    {
                        foreach (var cl in Clients)
                        {
                            Console.WriteLine(cl.Key.Trim() + " " + firstIp.Trim());
                            Console.WriteLine(cl.Value);
                            try
                            {
                                if (cl.Key.Trim() == firstIp.Trim())
                                {
                                    _logger.LogInformation($"Checking database for {cl.Key}");
                                    SendMessageToClientSecured(cl.Value, "0x007", val.topic, val.createUser, val.message, val.iD);
                                    // await SendMessageToClient(cl.Value, "0x007", val.topic, val.createUser, val.message, val.iD);
                                    // ReceiveConfimation(cl.Value);
                                    // ReceiveConfimationSecured(cl.Value);
                                    ReceiveConfimationSecured(cl.Value);
                                }
                            }
                            catch (NullReferenceException ex)
                            {
                                Console.WriteLine($"Nullable value: {firstIp}");
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }
                    await Task.Delay(500);
                }
            }   
        }

        private async Task ReceiveConfimation(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (message.Length > 0)
                {
                    string[] fullMessage = message.Split("<>");
                    ChangeSendDate(DateTime.Parse(fullMessage[1]), Int32.Parse(fullMessage[0]));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception receiving message from {client.Client.RemoteEndPoint}: {ex.Message}");
            }
        }

        private void ReceiveConfimationSecured(TcpClient client)
        {
            Stream stream;
            try
            {
                stream = client.GetStream();
                SslStream sslStream = new SslStream(stream, false);
                try
                {
                    byte[] buffer = new byte[2048];
                    sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);
                    int bytes = sslStream.Read(buffer, 0, buffer.Length);

                    string message = Encoding.UTF8.GetString(buffer, 0, bytes);
                    string[] messageParts = message.Split("<>");

                    if (message.Length > 0)
                    {
                        string[] fullMessage = message.Split("<>");
                        Console.WriteLine(fullMessage[0]);
                        ChangeSendDate(DateTime.Parse(fullMessage[1]), Int32.Parse(fullMessage[0]));
                    }
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    sslStream.Close();
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    sslStream.Close();
                }
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("Exception: {0}", e.InnerException.Message);
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



        private void HandleClient(TcpClient client)
        {
            string? clientIp = client.Client.RemoteEndPoint?.ToString()?.Split(':')[0];
            
            Clients.TryAdd(clientIp, client);
            _logger.LogInformation($"Client connected: {clientIp}");
        }

        private async Task SendMessageToClient(TcpClient client, string messageCode, string? topic = "", string? person = "", string? message = "", int? id = 0)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                string formatMessage = $"{messageCode.Trim()}<<<>>>{person.Trim()}<<<>>>{topic.Trim()}<<<>>>{message.Trim()}<<<>>>{id.ToString()}<<EOF>>";
                byte[] response = Encoding.UTF8.GetBytes(formatMessage);
                await stream.WriteAsync(response, 0, response.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception sending message to {client.Client.RemoteEndPoint}: {ex.Message}");
                if (client.Client.RemoteEndPoint != null)
                {
                    string clientIp = client.Client.RemoteEndPoint.ToString().Split(':')[0];
                    if (Clients.TryRemove(clientIp, out var _))
                    {
                        client.Close();
                        _logger.LogInformation($"Client removed due to error: {clientIp}");
                    }
                }
            }
        }

        private void SendMessageToClientSecured(TcpClient client, string messageCode, string? topic = "", string? person = "", string? message = "", int? id = 0)
        {
            try
            {
                SslStream sslStream = new SslStream(client.GetStream(), false);
                try
                {
                    sslStream.AuthenticateAsServer(serverCertificate, clientCertificateRequired: false, checkCertificateRevocation: true);
                    string formatMessage = $"{messageCode.Trim()}<<<>>>{person.Trim()}<<<>>>{topic.Trim()}<<<>>>{message.Trim()}<<<>>>{id.ToString()}<<EOF>>";
                    byte[] response = Encoding.UTF8.GetBytes(formatMessage);

                    sslStream.WriteAsync(response);
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    sslStream.Close();
                    return;
                }
                finally
                {
                    sslStream.Close();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                //_logger.LogError($"Exception sending message to {client.Client.RemoteEndPoint}: {ex.Message}");
                //if (client.Client.RemoteEndPoint != null)
                //{
                //    string clientIp = client.Client.RemoteEndPoint.ToString().Split(':')[0];
                //    if (Clients.TryRemove(clientIp, out var _))
                //    {
                //        client.Close();
                //        _logger.LogInformation($"Client removed due to error: {clientIp}");
                //    }
                //}
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

        public void ChangeSendDate(DateTime date, int id)
        {
            try
            {
                con.Open();

                string updateValue = "UPDATE base SET readdate = @date, [read] = 1 WHERE id = @thisId";
                SqlCommand updateSendDate = new SqlCommand(updateValue, con);

                updateSendDate.Parameters.AddWithValue("@date", date);
                updateSendDate.Parameters.AddWithValue("@thisId", id);

                updateSendDate.ExecuteNonQuery();

                con.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
