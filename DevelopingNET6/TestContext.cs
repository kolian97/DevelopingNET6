using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DevelopingNET6
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
    }
}
