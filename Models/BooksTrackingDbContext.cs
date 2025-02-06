using Microsoft.EntityFrameworkCore;

namespace BooksTracking.Models
{
    public class BooksTrackingDbContext : DbContext
    {
        public DbSet<Book> Books { get; set; }
        public DbSet<UserBook> UserBooks { get; set; }


        public BooksTrackingDbContext(DbContextOptions<BooksTrackingDbContext> options)
            : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Явно дефиниране на първичен ключ за UserBook, ако е необходимо
            modelBuilder.Entity<UserBook>()
                .HasKey(u => u.Id);
        }

    }

}
