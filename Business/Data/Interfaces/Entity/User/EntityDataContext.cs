using BusinessModels.People;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Business.Data.Interfaces.Entity.User;

public class EntityDataContext(DbContextOptions<EntityDataContext> options) : DbContext(options)
{
    public DbSet<UserModel> UserContext { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var builder = modelBuilder.Entity<UserModel>().ToCollection(nameof(UserModel));
    }
}

