//using System.Net;
//using System.Linq;
//using NUnit.Framework;
//namespace DevelopingNET60
//{
//    public class MockMessageSource : IMessageSource
//    {
//        private Queue<Message> messages = new();
//        private Dictionary<IPEndPoint, List<Message>> sentMessages = new();
//        private Server server;
//        private IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);

//        public MockMessageSource()
//        {
//            messages.Enqueue(new Message
//            {
//                Command = Command.Register,
//                FromName = "Вася"
//            });
//            messages.Enqueue(new Message
//            {
//                Command = Command.Register,
//                FromName = "Юля"
//            });
//            messages.Enqueue(new Message
//            {
//                Command = Command.Message,
//                FromName = "Юля",
//                ToName = "Вася",
//                Text = "От Юли"
//            });
//            messages.Enqueue(new Message
//            {
//                Command = Command.Message,
//                FromName = "Вася",
//                ToName = "Юля",
//                Text = "От Васи"
//            });
//        }

//        internal void AddServer(Server srv)
//        {
//            server = srv;
//        }

//        public Message Receive(ref IPEndPoint ep)
//        {
//            ep = endPoint;
//            if (messages.Count == 0)
//            {
//                server.Stop();
//                return null;
//            }
//            var msg = messages.Dequeue();
//            return msg;
//        }

//        public void Send(Message message, IPEndPoint ep)
//        {
//            if (!sentMessages.ContainsKey(ep))
//            {
//                sentMessages[ep] = new List<Message>();
//            }
//            sentMessages[ep].Add(message);
//        }

//        public IEnumerable<Message> GetSentMessages(IPEndPoint ep)
//        {
//            return sentMessages.ContainsKey(ep) ? sentMessages[ep] : Enumerable.Empty<Message>();
//        }
//    }
//    public class Tests
//    {
//        [SetUp]
//        public void Setup()
//        {
//            using (var ctx = new DevelopingNET60.TestContext())
//            {
//                ctx.Messages.RemoveRange(ctx.Messages);
//                ctx.Users.RemoveRange(ctx.Users);
//                ctx.SaveChanges();
//            }
//        }
//        [TearDown]
//        public void TeatDown()
//        {
//            using (var ctx = new DevelopingNET60.TestContext())
//            {
//                ctx.Messages.RemoveRange(ctx.Messages);
//                ctx.Users.RemoveRange(ctx.Users);
//                ctx.SaveChanges();
//            }
//        }
//        [Test]
//        public void TestUnreadMessages()
//        {
//            var mock = new MockMessageSource();
//            var srv = new Server(mock);
//            mock.AddServer(srv);

//            var serverThread = new Thread(srv.Work);
//            serverThread.Start();

//            using (var ctx = new TestContext())
//            {
//                ctx.Users.Add(new User { Name = "Вася" });
//                ctx.Users.Add(new User { Name = "Юля" });
//                ctx.SaveChanges();

//                ctx.Messages.Add(new Message
//                {
//                    FromUser = ctx.Users.First(u => u.Name == "Юля"),
//                    ToUser = ctx.Users.First(u => u.Name == "Вася"),
//                    Text = "Непрочитанное сообщение",
//                    Received = false
//                });
//                ctx.SaveChanges();
//            }

//            var ep = new IPEndPoint(IPAddress.Loopback, 0);
//            var unreadMessageRequest = new Message
//            {
//                Command = Command.List,
//                FromName = "Вася"
//            };

//            mock.Send(unreadMessageRequest, ep);

//            var sentMessages = mock.GetSentMessages(ep).ToList();

//            Assert.That(sentMessages.Count, Is.GreaterThan(0), "Сообщения не отправлены.");

//            Assert.That(sentMessages[0].Text, Is.EqualTo("Непрочитанное сообщение"));
//        }
//    }
//}