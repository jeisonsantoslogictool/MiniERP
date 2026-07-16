using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Compras;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedores");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Codigo).IsRequired().HasMaxLength(20);
        builder.Property(p => p.Nombre).IsRequired().HasMaxLength(150);

        builder.Property(p => p.TipoDocumento).HasConversion<int>();
        builder.Property(p => p.NumeroDocumento).HasMaxLength(20);
        builder.Property(p => p.Telefono).HasMaxLength(20);
        builder.Property(p => p.Email).HasMaxLength(150);
        builder.Property(p => p.Direccion).HasMaxLength(250);
        builder.Property(p => p.Contacto).HasMaxLength(150);

        builder.Property(p => p.BalanceActual).HasPrecision(18, 2);

        builder.Property(p => p.CreadoPor).HasMaxLength(450);
        builder.Property(p => p.ModificadoPor).HasMaxLength(450);

        builder.HasIndex(p => p.Codigo).IsUnique();
        builder.HasIndex(p => p.Nombre);

        // Mismo criterio que en clientes: el proveedor informal puede no traer documento,
        // asi que el nulo debe poder repetirse.
        builder.HasIndex(p => p.NumeroDocumento)
            .IsUnique()
            .HasFilter("[NumeroDocumento] IS NOT NULL");

        builder.Ignore(p => p.TieneDeuda);
    }
}

public class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> builder)
    {
        builder.ToTable("Compras");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Numero).IsRequired().HasMaxLength(20);
        builder.Property(c => c.NcfProveedor).HasMaxLength(19);
        builder.Property(c => c.Observacion).HasMaxLength(500);

        builder.Property(c => c.Estado).HasConversion<int>();
        builder.Property(c => c.Condicion).HasConversion<int>();

        builder.Property(c => c.Subtotal).HasPrecision(18, 2);
        builder.Property(c => c.Itbis).HasPrecision(18, 2);
        builder.Property(c => c.Total).HasPrecision(18, 2);

        builder.Property(c => c.CreadoPor).HasMaxLength(450);
        builder.Property(c => c.ModificadoPor).HasMaxLength(450);

        builder.HasIndex(c => c.Numero).IsUnique();
        builder.HasIndex(c => new { c.Estado, c.Fecha });
        builder.HasIndex(c => new { c.ProveedorId, c.Fecha });

        builder.HasOne(c => c.Proveedor)
            .WithMany(p => p.Compras)
            .HasForeignKey(c => c.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Las lineas no existen sin su compra: se borran con ella.
        builder.HasMany(c => c.Lineas)
            .WithOne(l => l.Compra)
            .HasForeignKey(l => l.CompraId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.EsEditable);
        builder.Ignore(c => c.EstaRecibida);
    }
}

public class LineaCompraConfiguration : IEntityTypeConfiguration<LineaCompra>
{
    public void Configure(EntityTypeBuilder<LineaCompra> builder)
    {
        builder.ToTable("LineasCompra");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Descripcion).IsRequired().HasMaxLength(200);

        builder.Property(l => l.Cantidad).HasPrecision(18, 3);
        builder.Property(l => l.CostoUnitario).HasPrecision(18, 4);
        builder.Property(l => l.TasaItbis).HasPrecision(5, 4);

        builder.Property(l => l.CreadoPor).HasMaxLength(450);
        builder.Property(l => l.ModificadoPor).HasMaxLength(450);

        builder.HasOne(l => l.Producto)
            .WithMany()
            .HasForeignKey(l => l.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Subtotal, Itbis y Total se derivan de cantidad y costo: no ocupan columna.
        builder.Ignore(l => l.Subtotal);
        builder.Ignore(l => l.Itbis);
        builder.Ignore(l => l.Total);
    }
}
