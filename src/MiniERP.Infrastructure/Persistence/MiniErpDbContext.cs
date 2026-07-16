using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence;

/// <summary>
/// Contexto unico del sistema. Agrupa la identidad y los cinco modulos de negocio
/// (inventario, ventas, compras, clientes y finanzas) en un solo historial de
/// migraciones, para que ambos desarrolladores trabajen sobre una sola linea de tiempo.
/// </summary>
public class MiniErpDbContext(DbContextOptions<MiniErpDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    // Inventario
    public DbSet<Producto> Productos => Set<Producto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<UnidadMedida> UnidadesMedida => Set<UnidadMedida>();
    public DbSet<MovimientoInventario> MovimientosInventario => Set<MovimientoInventario>();

    // Clientes
    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MiniErpDbContext).Assembly);
    }
}
