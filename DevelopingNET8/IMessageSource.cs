
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace UnitTest1
{
    public enum Command
    {
        Register,
        Message,
        Confirmation,
        List
    }

    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; } = null!;
        public bool Received { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }
        public virtual User FromUser { get; set; } = null!;
        public virtual User ToUser { get; set; } = null!;

        public Command Command { get; set; }
        public string FromName { get; set; } = null!;
        public string ToName { get; set; } = null!;

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }
        public static Message FromJson(string json)
        {
            return JsonSerializer.Deserialize<Message>(json);
        }
    }
    interface IMessageSource
    {
        void Send(Message message, IPEndPoint ep);
        Message Receive(ref IPEndPoint ep);
    }
    class UdpMessageSource : IMessageSource
    {
        private UdpClient udpClient;
        public UdpMessageSource()
        {
            udpClient = new UdpClient(12345);
        }
        public Message Receive(ref IPEndPoint ep)
        {
            byte[] receiveBytes = udpClient.Receive(ref ep);
            string receivedData = Encoding.ASCII.GetString(receiveBytes);
            return Message.FromJson(receivedData);
        }
        public void Send(Message message, IPEndPoint ep)
        {
            byte[] forwardBytes = Encoding.ASCII.GetBytes(message.ToJson());
            udpClient.Send(forwardBytes, forwardBytes.Length, ep);
        }
    }
    class Server
    {
        Dictionary<string, IPEndPoint> clients = new Dictionary<string, IPEndPoint>();
        IMessageSource messageSource;
        private bool isRunning;

        public Server(IMessageSource source)
        {
            messageSource = source;
            isRunning = true;  
        }

        void Register(Message message, IPEndPoint fromep)
        {
            Console.WriteLine("Message Register, name = " + message.FromName);
            clients.Add(message.FromName, fromep);
            using (var ctx = new TestContext())
            {
                if (ctx.Users.FirstOrDefault(x => x.Name == message.FromName) != null) return;
                ctx.Add(new User { Name = message.FromName });
                ctx.SaveChanges();
            }
        }

        void ConfirmMessageReceived(int? id)
        {
            Console.WriteLine("Message confirmation id=" + id);
            using (var ctx = new TestContext())
            {
                var msg = ctx.Messages.FirstOrDefault(x => x.Id == id);
                if (msg != null)
                {
                    msg.Received = true;
                    ctx.SaveChanges();
                }
            }
        }

        void RelayMessage(Message message)
        {
            int? id = null;
            if (clients.TryGetValue(message.ToName, out IPEndPoint ep))
            {
                using (var ctx = new TestContext())
                {
                    var fromUser = ctx.Users.First(x => x.Name == message.FromName);
                    var toUser = ctx.Users.First(x => x.Name == message.ToName);
                    var msg = new Message
                    {
                        FromUser = fromUser,
                        ToUser = toUser,
                        Received = false,
                        Text = message.Text
                    };
                    ctx.Messages.Add(msg);
                    ctx.SaveChanges();
                    id = msg.Id;
                }
                var forwardMessage = new Message()
                {
                    Id = (int)id,
                    Command = Command.Message,
                    ToName = message.ToName,
                    FromName = message.FromName,
                    Text = message.Text
                };
                messageSource.Send(forwardMessage, ep);
                Console.WriteLine($"Message Relayed, from = {message.FromName} to = {message.ToName}");
            }
            else
            {
                Console.WriteLine("Пользователь не найден.");
            }
        }

        void ProcessMessage(Message message, IPEndPoint fromep)
        {
            Console.WriteLine($"Получено сообщение от {message.FromName} для {message.ToName} с командой {message.Command}:");
            Console.WriteLine(message.Text);
            if (message.Command == Command.Register)
            {
                Register(message, new IPEndPoint(fromep.Address, fromep.Port));
            }
            if (message.Command == Command.Confirmation)
            {
                Console.WriteLine("Confirmation received");
                ConfirmMessageReceived(message.Id);
            }
            if (message.Command == Command.Message)
            {
                RelayMessage(message);
            }
        }
        public void Stop()
        {
            Console.WriteLine("Сервер остановлен.");
            isRunning = false;
        }

        public void Work()
        {
            Console.WriteLine("UDP сервер ожидает сообщений...");
            while (isRunning)  
            {
                try
                {
                    IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    var message = messageSource.Receive(ref remoteEndPoint);
                    if (message != null)
                    {
                        ProcessMessage(message, remoteEndPoint);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при обработке сообщения: " + ex.Message);
                }
            }
        }
    }
}
