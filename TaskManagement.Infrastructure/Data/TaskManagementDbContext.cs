using Microsoft.EntityFrameworkCore;
using TaskManagement.Core.Entities;

namespace TaskManagement.Infrastructure.Data
{
    public class TaskManagementDbContext : DbContext
    {
        public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TaskItemTag> TaskItemTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure TaskItem entity
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.ToTable("TaskItems");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Telephone).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Priority).IsRequired();
                entity.Property(e => e.DueDate).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Configure Tag entity
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.ToTable("Tags");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure TaskItemTag (many-to-many relationship)
            modelBuilder.Entity<TaskItemTag>(entity =>
            {
                entity.ToTable("TaskItemTags");
                entity.HasKey(e => new { e.TaskItemId, e.TagId });

                entity.HasOne(e => e.TaskItem)
                    .WithMany(t => t.TaskItemTags)
                    .HasForeignKey(e => e.TaskItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Tag)
                    .WithMany(t => t.TaskItemTags)
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed some initial tags
            modelBuilder.Entity<Tag>().HasData(
                new Tag { Id = 1, Name = "Urgent" },
                new Tag { Id = 2, Name = "Important" },
                new Tag { Id = 3, Name = "Work" },
                new Tag { Id = 4, Name = "Personal" },
                new Tag { Id = 5, Name = "Home" },
                new Tag { Id = 6, Name = "Shopping" },
                new Tag { Id = 7, Name = "Meeting" },
                new Tag { Id = 8, Name = "Project" },
                new Tag { Id = 9, Name = "Research" },
                new Tag { Id = 10, Name = "Development" }
            );
        }
    }
}
