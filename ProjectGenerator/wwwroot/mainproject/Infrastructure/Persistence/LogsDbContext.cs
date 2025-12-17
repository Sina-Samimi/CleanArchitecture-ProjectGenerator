using Attar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Attar.Infrastructure.Persistence;

/// <summary>
/// DbContext جداگانه برای دیتابیس لاگ‌ها
/// این دیتابیس جداگانه است تا Performance دیتابیس اصلی تحت تأثیر قرار نگیرد
/// </summary>
public sealed class LogsDbContext : DbContext
{
    public LogsDbContext(DbContextOptions<LogsDbContext> options)
        : base(options)
    {
    }

    public DbSet<AttarApplicationLog> AttarApplicationLogs => Set<AttarApplicationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // فقط Configuration مربوط به AttarApplicationLog را اعمال کن
        modelBuilder.ApplyConfiguration(new Configurations.AttarApplicationLogConfiguration());
    }
}

