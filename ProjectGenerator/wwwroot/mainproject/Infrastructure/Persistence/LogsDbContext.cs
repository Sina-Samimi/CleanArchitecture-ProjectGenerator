using MobiRooz.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MobiRooz.Infrastructure.Persistence;

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

    public DbSet<ApplicationLog> ApplicationLogs => Set<ApplicationLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // فقط Configuration مربوط به ApplicationLog را اعمال کن
        modelBuilder.ApplyConfiguration(new Configurations.ApplicationLogConfiguration());
    }
}

