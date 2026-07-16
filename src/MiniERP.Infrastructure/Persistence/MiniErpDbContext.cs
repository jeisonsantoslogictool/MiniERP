using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MiniERP.Infrastructure.Persistence;

/// <summary>
/// Contexto unico del sistema. Agrupa la identidad y los cinco modulos de negocio
/// (inventario, ventas, compras, clientes y finanzas) en un solo historial de
/// migraciones, para que ambos desarrolladores trabajen sobre una sola linea de tiempo.
/// </summary>
public class MiniErpDbContext(DbContextOptions<MiniErpDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MiniErpDbContext).Assembly);
    }
}
