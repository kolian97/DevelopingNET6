using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;

namespace DevelopingNET6
{
    public class Server
    {
        private readonly Dictionary<string, IPEndPoint> clients1 = new();
        private UdpClient udpClient;
        private readonly IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Loopback, 12345);

        void Register(Message message, IPEndPoint fromEp)
        {
            Console.WriteLine($"Регистрация пользователя: {message.FromName}");
            clients1[message.FromName] = fromEp;

            using (var ctx = new TestContext())
            {
                if (ctx.Users.FirstOrDefault(x => x.Name == message.FromName) != null)
                    return;

                ctx.Add(new User { Name = message.FromName });
                ctx.SaveChanges();
            }
        }

        void ConfirmMessageReceived(int? id)
        {
            Console.WriteLine($"Подтверждение получения сообщения, id = {id}");

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
            if (!clients.TryGetValue(message.ToName, out var ep))
            {
                Console.WriteLine("Пользователь не найден.");
                return;
            }

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

                message.Id = msg.Id;
            }

            var forwardMessageJson = message.ToJson();
            byte[] forwardBytes = Encoding.ASCII.GetBytes(forwardMessageJson);
            udpClient.Send(forwardBytes, forwardBytes.Length, ep);

            Console.WriteLine($"Сообщение отправлено от {message.FromName} к {message.ToName}");
        }
        void SendUnreadMessages(string userName, IPEndPoint clientEndPoint)
        {
            Console.WriteLine($"Запрос непрочитанных сообщений для пользователя: {userName}");

            using (var ctx = new TestContext())
            {
                var user = ctx.Users.FirstOrDefault(u => u.Name == userName);

                if (user == null)
                {
                    Console.WriteLine($"Пользователь {userName} не найден.");
                    return;
                }

                var unreadMessages = ctx.Messages
                    .Where(m => m.ToUserId == user.Id && !m.Received)
                    .ToList();

                foreach (var msg in unreadMessages)
                {
                    msg.Received = true;

                    var forwardMessage = new Message
                    {
                        Command = Command.Message,
                        Id = msg.Id,
                        FromName = msg.FromUser.Name,
                        ToName = msg.ToUser.Name,
                        Text = msg.Text,
                        Received = true
                    };

                    string forwardMessageJson = forwardMessage.ToJson();
                    byte[] forwardBytes = Encoding.ASCII.GetBytes(forwardMessageJson);

                    udpClient.Send(forwardBytes, forwardBytes.Length, clientEndPoint);
                }
                ctx.SaveChanges();

                Console.WriteLine($"Отправлено {unreadMessages.Count} сообщений пользователю {userName}.");
            }
        }
        void RequestUnreadMessages()
        {
            var listRequest = new Message
            {
                Command = Command.List,
                FromName = "UserName"
            };

            string requestJson = listRequest.ToJson();
            byte[] requestBytes = Encoding.ASCII.GetBytes(requestJson);

            udpClient.Send(requestBytes, requestBytes.Length, serverEndPoint);
            Console.WriteLine("Запрос непрочитанных сообщений отправлен.");
        }

        void ProcessMessage(Message message, IPEndPoint fromep)
        {
            Console.WriteLine($"Получено сообщение от {message.FromName} для {message.ToName} с командой {message.Command}:");
            Console.WriteLine(message.Text);

            switch (message.Command)
            {
                case Command.Register:
                    Register(message, new IPEndPoint(fromep.Address, fromep.Port));
                    break;

                case Command.Confirmation:
                    Console.WriteLine("Confirmation received");
                    ConfirmMessageReceived(message.Id);
                    break;

                case Command.Message:
                    RelayMessage(message);
                    break;

                case Command.List:
                    SendUnreadMessages(message.FromName, fromep);
                    break;

                default:
                    Console.WriteLine("Неизвестная команда.");
                    break;
            }
        }

        public void Work()
        {
            udpClient = new UdpClient(12345);
            Console.WriteLine("Сервер запущен и ожидает сообщений...");

            while (true)
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] receiveBytes = udpClient.Receive(ref remoteEndPoint);
                string receivedData = Encoding.ASCII.GetString(receiveBytes);

                try
                {
                    var message = Message.FromJson(receivedData);
                    ProcessMessage(message, remoteEndPoint);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке сообщения: {ex.Message}");
                }
            }
        }
    }
}
