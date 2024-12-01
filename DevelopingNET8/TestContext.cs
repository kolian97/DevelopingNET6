using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Net;
using Grpc.Core;
using Assert = NUnit.Framework.Assert;

namespace UnitTest1
{
    public partial class TestContext : DbContext
    {
        public virtual DbSet<Message> Messages { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .LogTo(Console.WriteLine)
                .UseLazyLoadingProxies()
                .UseNpgsql("Host=localhost;Username=postgres;Password=example;Database=chatv1");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("messages_pkey");
                entity.ToTable("messages");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Text).HasColumnName("text");
                entity.Property(e => e.FromUserId).HasColumnName("from_user_id");
                entity.Property(e => e.ToUserId).HasColumnName("to_user_id");
                entity.Property(e => e.Received).HasColumnName("received");
                entity.HasOne(d => d.FromUser).WithMany(p => p.FromMessages)
                    .HasForeignKey(d => d.FromUserId)
                    .HasConstraintName("messages_from_user_id_fkey");
                entity.HasOne(d => d.ToUser).WithMany(p => p.ToMessages)
                    .HasForeignKey(d => d.ToUserId)
                    .HasConstraintName("messages_to_user_id_fkey");
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id).HasName("users_pkey");
                entity.ToTable("users");
                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name)
                    .HasMaxLength(255)
                    .HasColumnName("name");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
        [Test]
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

            Assert.That(sentMessages.Count, Is.GreaterThan(0), "Сообщения не отправлены.");

            NUnit.Framework.Assert.That(sentMessages[0].Text, Is.EqualTo("Непрочитанное сообщение"));
        }
    }
}