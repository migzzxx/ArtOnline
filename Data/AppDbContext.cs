using Microsoft.EntityFrameworkCore;
using ArtOnline.Models;

namespace ArtOnline.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Commission> Commissions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }
}
