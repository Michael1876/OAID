using AtsBillingSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AtsBillingSystem.Data.Context
{
    public class AppDbContext : DbContext
    {
        public DbSet<SubscriberEntity> Subscribers { get; set; } = null!;
        public DbSet<TariffEntity> Tariffs { get; set; } = null!;
        public DbSet<CallRecordEntity> CallRecords { get; set; } = null!;
        public DbSet<AdminUserEntity> AdminUsers { get; set; } = null!;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Ограничения и индексы
            modelBuilder.Entity<SubscriberEntity>()
                .HasIndex(s => s.PhoneNumber)
                .IsUnique(); // Номер телефона уникален

            modelBuilder.Entity<AdminUserEntity>()
                .HasIndex(u => u.Login)
                .IsUnique();

            // Настройка Concurrency Token (Оптимистическая блокировка)
            // Если используешь PostgreSQL (Npgsql), для byte[] иногда настраивают скрытый uint xmin, 
            // но классический EF паттерн выглядит так:
            modelBuilder.Entity<SubscriberEntity>()
                .Property(s => s.RowVersion)
                .IsRowVersion();

            // Data Seeding: Первичное наполнение
            var defaultTariffId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            modelBuilder.Entity<TariffEntity>().HasData(new TariffEntity
            {
                Id = defaultTariffId,
                Name = "Базовый",
                InternalMinutePrice = 1.50m,
                CityMinutePrice = 3.00m,
                ConnectionFee = 0,
                SubscriptionFee = 150.00m,
                IsArchived = false
            });

            // Пароль "admin" (в реальности здесь должен быть реальный хэш от PBKDF2, это пример)
            modelBuilder.Entity<AdminUserEntity>().HasData(new AdminUserEntity
            {
                Id = Guid.NewGuid(),
                Login = "admin",
                PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918" // SHA-256 ("admin")
            });
        }
    }
}