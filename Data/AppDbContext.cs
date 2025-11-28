using Microsoft.EntityFrameworkCore;
using TodoApp.Api.Entities.Models;

namespace TodoApp.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    }

}
