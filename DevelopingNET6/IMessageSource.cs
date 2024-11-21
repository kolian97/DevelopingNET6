using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using DevelopingNET6;
using DevelopingNET6.Model;
namespace DevelopingNET6
{
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
        Dictionary<String, IPEndPoint> clients = new Dictionary<string,
        IPEndPoint>();
        IMessageSource messageSource;
        public Server(IMessageSource source)
        {
            messageSource = source;
        }
        void Register(Message message, IPEndPoint fromep)
        {
            Console.WriteLine("Message Register, name = " + message.FromName);
            clients.Add(message.FromName, fromep);
            using (var ctx = new TestContext())
            {
                if (ctx.Users.FirstOrDefault(x => x.Name == message.FromName)
                != null) return;
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
        void RelyMessage(Message message)
        {
            int? id = null;
            if (clients.TryGetValue(message.ToName, out IPEndPoint ep))
            {
                using (var ctx = new TestContext())
                {
                    var fromUser = ctx.Users.First(x => x.Name ==
                    message.FromName);
                    var toUser = ctx.Users.First(x => x.Name ==
                    message.ToName);
                    var msg = new DevelopingNET6.Model.Message
                    {
                        FromUser
                    = fromUser,
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
                    Id = id,
                    Command =
                Command.Message,
                    ToName = message.ToName,
                    FromName = message.FromName,
                    Text =
                message.Text
                };
                messageSource.Send(forwardMessage, ep);
                Console.WriteLine($"Message Relied, from = {message.FromName} to = { message.ToName}");
            }
            else
            {
                Console.WriteLine("Пользователь не найден.");
            }
        }
        void ProcessMessage(Message message, IPEndPoint fromep)
        {
            Console.WriteLine($"Получено сообщение от {message.FromName} для { message.ToName} с командой { message.Command}:");
        Console.WriteLine(message.Text);
            if (message.Command == Command.Register)
            {
                Register(message, new IPEndPoint(fromep.Address,
                fromep.Port));
            }
            if (message.Command == Command.Confirmation)
            {
                Console.WriteLine("Confirmation receiver");
                ConfirmMessageReceived(message.Id);
            }
            if (message.Command == Command.Message)
            {
                RelyMessage(message);
            }
        }
        public void Work()
        {
            Console.WriteLine("UDP Клиент ожидает сообщений...");
            while (true)
            {
                try
                {
                    IPEndPoint remoteEndPoint = new
                    IPEndPoint(IPAddress.Any, 0);
                    var message = messageSource.Receive(ref remoteEndPoint);
                    ProcessMessage(message, remoteEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка при обработке сообщения: " + ex.Message);
                }
            }
        }
    }
}
