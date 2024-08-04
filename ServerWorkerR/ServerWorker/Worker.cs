using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Data.SqlClient;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Net.Security;
using System.Threading.Tasks;

namespace ServerWorker
{
    public class Worker : BackgroundService
    {
        // Database
        public string connString = string.Empty;
        public SqlConnection con;
        public List<SendToValue> pcToSend = new List<SendToValue>();

        // Server setup
        private int Port { get; set; }
        private IPAddress LocalAddr { get; set; }
        private static readonly ConcurrentDictionary<string, (TcpClient client, SslStream sslStream)> Clients = new ConcurrentDictionary<string, (TcpClient client, SslStream sslStream)>();
        private TcpListener _server;
        private Timer _timer;

        // Others
        private readonly ILogger<Worker> _logger;
        private readonly IConfiguration _configuration;
        private X509Certificate serverCertificate;
        private string certificatePath = String.Empty;

        // Interval between checks
        private int Interval;

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
            certificatePath = workerConfig.Certificate;
            Interval = workerConfig.CheckInterval;

            // Setup database
            con = new SqlConnection(connString);

            // Setup server
            serverCertificate = new X509Certificate2(certificatePath, "1qaz@WSX");

            _server = new TcpListener(LocalAddr, Port);
            _server.Start();
            _logger.LogInformation("Server started. Waiting for connections...");

            stoppingToken.Register(() =>
            {
                _logger.LogInformation("Shutting down server...");
                _server.Stop();
                con.Close();
            });

            _timer = new Timer(_ => PeriodicTask(), null, TimeSpan.Zero, TimeSpan.FromSeconds(Interval));

            while (!stoppingToken.IsCancellationRequested)
            {
                if (_server.Pending())
                {
                    TcpClient client = await _server.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleClient(client));
                }
                await Task.Delay(500, stoppingToken);
            }
        }

        // Background task
        private async Task PeriodicTask()
        {
            PrintClients();
            try
            {
                GetValuesFromDatabase();
                CheckToDelete();
                foreach (var clientEntry in Clients)
                {
                    string clientIp = clientEntry.Key.Split(":")[0];
                    var (client, sslStream) = clientEntry.Value;

                    var messagesForClient = pcToSend.Where(m => GetIpFromPcName(m.pcName) == clientIp).ToList();

                    foreach (var message in messagesForClient)
                    {
                        try
                        {
                            await SendMessageToClientSecured(sslStream, "0x007", message.topic, message.createUser, message.message, message.iD);
                            await ReceiveConfirmationSecured(sslStream, message.iD);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Exception sending message to {clientIp}: {ex.Message}");
                            RemoveClient(clientIp, sslStream, client);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception during periodic task: {ex.Message}");
            }
        }


        private async Task HandleClient(TcpClient client)
        {
            string? clientIp = client.Client.RemoteEndPoint?.ToString();
            if (clientIp != null)
            {
                SslStream sslStream = new SslStream(client.GetStream(), false);
                try
                {
                    await sslStream.AuthenticateAsServerAsync(serverCertificate, clientCertificateRequired: false, SslProtocols.Tls12, checkCertificateRevocation: true);

                    Clients.TryAdd(clientIp, (client, sslStream));
                    _logger.LogInformation($"Client connected: {clientIp}");

                    while (client.Connected)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(15));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Exception handling client {clientIp}: {ex.Message}");
                    RemoveClient(clientIp, sslStream, client);
                }
            }
        }


        // Talking to clients
        private async Task ReceiveConfirmationSecured(SslStream sslStream, int id)
        {
            try
            {
                byte[] buffer = new byte[2048];
                int bytes = await sslStream.ReadAsync(buffer, 0, buffer.Length);

                if (bytes > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytes);
                    string[] fullMessage = message.Split("<>");
                    if (fullMessage.Length >= 2)
                    {
                        ChangeSendDate(id, DateTime.Parse(fullMessage[1]));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception receiving message: {ex.Message}");
                throw;
            }
        }

        private async Task SendMessageToClientSecured(SslStream sslStream, string messageCode, string? topic = "", string? person = "", string? message = "", int? id = 0)
        {
            try
            {
                string formatMessage = $"{messageCode.Trim()}<<<>>>{person.Trim()}<<<>>>{topic.Trim()}<<<>>>{message.Trim()}<<<>>>{id.ToString()}<<EOF>>";
                byte[] response = Encoding.UTF8.GetBytes(formatMessage);

                await sslStream.WriteAsync(response, 0, response.Length);
            }
            catch (AuthenticationException e)
            {
                _logger.LogError($"Authentication failed: {e.Message}");
                if (e.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {e.InnerException.Message}");
                }
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception sending message: {ex.Message}");
                throw;
            }
        }


        // Database
        public void ChangeSendDate(int id, DateTime date)
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
                _logger.LogError(ex.ToString());
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
                _logger.LogError(ex.ToString());
            }
        }


        // Managing
        private string GetIpFromPcName(string pcName)
        {
            Console.WriteLine(pcName);
            try
            {
                IPAddress[] addresslist = Dns.GetHostAddresses(pcName.Trim());
                var ip = addresslist.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork)?.ToString();
                Console.WriteLine(ip);
                return ip;
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return string.Empty;
            }
        }


        private void PrintClients()
        {
            foreach (var kvp in Clients)
            {
                string clientIp = kvp.Key;
                var (client, sslStream) = kvp.Value;

                bool isConnected = client.Connected;
                _logger.LogInformation($"Client IP: {clientIp}, Connected: {isConnected}");
                _logger.LogInformation(DateTime.Now.ToString());
            }
        }


        private void RemoveClient(string clientIp, SslStream sslStream, TcpClient client)
        {
            if (Clients.TryRemove(clientIp, out var _))
            {
                sslStream.Close();
                client.Close();
                _logger.LogInformation($"Client removed: {clientIp}");
            }
        }


        private void CheckToDelete()
        {
            foreach (var kvp in Clients)
            {
                string clientIp = kvp.Key;
                var (client, sslStream) = kvp.Value;

                if (!SocketConnected(client.Client))
                {
                    RemoveClient(clientIp, sslStream, client);
                }
            }
        }

        bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }
    }
}
