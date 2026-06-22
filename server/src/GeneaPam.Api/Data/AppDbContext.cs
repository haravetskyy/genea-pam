using Microsoft.EntityFrameworkCore;

namespace GeneaPam.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
