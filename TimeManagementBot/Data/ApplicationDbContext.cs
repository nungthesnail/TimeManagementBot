using Microsoft.EntityFrameworkCore;
using TimeManagementBot.Models;

namespace TimeManagementBot.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var taskEntity = modelBuilder.Entity<TaskItem>();
        taskEntity.ToTable("tasks");
        taskEntity.HasKey(static x => x.Id);
        taskEntity
            .Property(static x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();
        taskEntity
            .Property(static x => x.Description)
            .HasColumnName("description")
            .HasMaxLength(100)
            .IsRequired();
        taskEntity
            .Property(static x => x.IsCompleted)
            .HasColumnName("completed")
            .HasDefaultValue(false)
            .IsRequired();
        taskEntity
            .Property(static x => x.ChatId)
            .HasColumnName("chat")
            .IsRequired();
    }
}
