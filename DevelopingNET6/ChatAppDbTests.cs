using System.Net;
using DevelopingNET6;
namespace DevelopingNET6
{
    public class MockMessageSource : IMessageSource
    {
        private Queue<Message> messages = new();
        private Server server;
        private IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        public MockMessageSource()
        {
            messages.Enqueue(new Message
            {
                Command = Command.Register,
                FromName = "Вася"
            });
            messages.Enqueue(new Message
            {
                Command = Command.Register,
                FromName = "Юля"
            });
            messages.Enqueue(new Message
            {
                Command = Command.Message,
                FromName = "Юля",
                ToName = "Вася",
                Text = "От Юли"
            });
            messages.Enqueue(new Message
            {
                Command = Command.Message,
                FromName = "Вася",
                ToName = "Юля",
                Text = "От Васи"
            });
        }
        public void AddServer(Server srv)
        {
            server = srv;
        }
        public Message Receive(ref IPEndPoint ep)
        {
            ep = endPoint;
            if (messages.Count == 0)
            {
                server.Stop();
                return null;
            }
            var msg = messages.Dequeue();
            return msg;
        }
        public void Send(Message message, IPEndPoint ep)
        {
            if (!sentMessages.ContainsKey(ep))
            {
                sentMessages[ep] = new List<Message>();
            }
            sentMessages[ep].Add(message);
        }
        public IEnumerable<Message> GetSentMessages(IPEndPoint ep)
        {
            return sentMessages.ContainsKey(ep) ? sentMessages[ep] : Enumerable.Empty<Message>();
        }

        void IMessageSource.Send(Message message, IPEndPoint ep)
        {
            throw new NotImplementedException();
        }
    }
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            using (var ctx = new evelopingNET6.Model.TestContext())
            {
                ctx.Messages.RemoveRange(ctx.Messages);
                ctx.Users.RemoveRange(ctx.Users);
                ctx.SaveChanges();
            }
        }
        [TearDown]
        public void TeatDown()
        {
            using (var ctx = new evelopingNET6.Model.TestContext())
            {
                ctx.Messages.RemoveRange(ctx.Messages);
                ctx.Users.RemoveRange(ctx.Users);
                ctx.SaveChanges();
            }
        }
        [Test]
        public void Test1()
        {
            var mock = new MockMessageSource();
            var srv = new Server(mock);
            mock.AddServer(srv);
            srv.Work();
            using (var ctx = new evelopingNET6.Model.TestContext())
            {
                Assert.IsTrue(ctx.Users.Count() == 2, "Пользователи не созданы");
                var user1 = ctx.Users.FirstOrDefault(x => x.Name == "Вася");
                var user2 = ctx.Users.FirstOrDefault(x => x.Name == "Юля");
                Assert.IsNotNull(user1, "Пользователь не созданы");
                Assert.IsNotNull(user2, "Пользователь не созданы");
                Assert.IsTrue(user1.FromMessages.Count == 1);
                Assert.IsTrue(user2.FromMessages.Count == 1);
                Assert.IsTrue(user1.ToMessages.Count == 1);
                Assert.IsTrue(user2.ToMessages.Count == 1);
                var msg1 = ctx.Messages.FirstOrDefault(x => x.FromUser == user1 &&
                x.ToUser == user2);
                var msg2 = ctx.Messages.FirstOrDefault(x => x.FromUser == user2 &&
                x.ToUser == user1);
                Assert.AreEqual("От Юли", msg2.Text);
                Assert.AreEqual("От Васи", msg1.Text);
            }
            [Test2]
            public void TestUnreadMessages()
            {
                var mock = new MockMessageSource();
                var srv = new Server(mock);
                mock.AddServer(srv);
                var serverThread = new Thread(srv.Work);
                serverThread.Start();

                using (var ctx = new TestContext())
                {
                    ctx.Users.Add(new User { Name = "Вася" });
                    ctx.Users.Add(new User { Name = "Юля" });
                    ctx.SaveChanges();

                    ctx.Messages.Add(new Message
                    {
                        FromUser = ctx.Users.First(u => u.Name == "Юля"),
                        ToUser = ctx.Users.First(u => u.Name == "Вася"),
                        Text = "Непрочитанное сообщение",
                        Received = false
                    });
                    ctx.SaveChanges();
                }

                var ep = new IPEndPoint(IPAddress.Loopback, 0);
                var unreadMessageRequest = new Message
                {
                    Command = Command.List,
                    FromName = "Вася"
                };

                mock.Send(unreadMessageRequest, ep);
                var sentMessages = mock.GetSentMessages(ep).ToList();
                Assert.IsTrue(sentMessages.Count > 0, "Сообщения не отправлены.");
                Assert.AreEqual("Непрочитанное сообщение", sentMessages[0].Text);
            }
        }
    }
}