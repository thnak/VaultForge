using BusinessModels.People;
using Microsoft.EntityFrameworkCore;

namespace Business.Data.Interfaces.Entity.User;

public class EntityDataContext(DbContextOptions<EntityDataContext> options) : DbContext(options)
{
    public DbSet<UserModel> UserContext { get; set; }
}

