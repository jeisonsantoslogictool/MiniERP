using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Finanzas;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Ventas;

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
    public DbSet<Cobro> Cobros => Set<Cobro>();

    // Ventas
    public DbSet<SecuenciaNcf> SecuenciasNcf => Set<SecuenciaNcf>();
    public DbSet<Factura> Facturas => Set<Factura>();
    public DbSet<LineaFactura> LineasFactura => Set<LineaFactura>();

    // Compras
    public DbSet<Proveedor> Proveedores => Set<Proveedor>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<LineaCompra> LineasCompra => Set<LineaCompra>();
    public DbSet<Pago> Pagos => Set<Pago>();
    public DbSet<DevolucionCompra> DevolucionesCompra => Set<DevolucionCompra>();
    public DbSet<LineaDevolucionCompra> LineasDevolucionCompra => Set<LineaDevolucionCompra>();

    // Finanzas
    public DbSet<CategoriaEgreso> CategoriasEgreso => Set<CategoriaEgreso>();
    public DbSet<Egreso> Egresos => Set<Egreso>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(MiniErpDbContext).Assembly);
    }
}
