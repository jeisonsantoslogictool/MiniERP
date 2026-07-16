using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Ventas;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> builder)
    {
        builder.ToTable("Facturas");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Numero).IsRequired().HasMaxLength(20);
        builder.Property(f => f.Ncf).HasMaxLength(19);
        builder.Property(f => f.ClienteNombre).IsRequired().HasMaxLength(150);
        builder.Property(f => f.ClienteDocumento).HasMaxLength(20);
        builder.Property(f => f.MotivoAnulacion).HasMaxLength(250);
        builder.Property(f => f.UsuarioId).HasMaxLength(450);

        builder.Property(f => f.Estado).HasConversion<int>();
        builder.Property(f => f.Condicion).HasConversion<int>();
        builder.Property(f => f.TipoComprobante).HasConversion<int>();

        builder.Property(f => f.Subtotal).HasPrecision(18, 2);
        builder.Property(f => f.Itbis).HasPrecision(18, 2);
        builder.Property(f => f.Total).HasPrecision(18, 2);
        builder.Property(f => f.CostoTotal).HasPrecision(18, 2);
        builder.Property(f => f.MontoRecibido).HasPrecision(18, 2);
        builder.Property(f => f.Cambio).HasPrecision(18, 2);

        builder.Property(f => f.CreadoPor).HasMaxLength(450);
        builder.Property(f => f.ModificadoPor).HasMaxLength(450);

        builder.HasIndex(f => f.Numero).IsUnique();

        // Dos facturas no pueden compartir NCF: es la garantia final contra el duplicado,
        // por debajo de la asignacion atomica. El filtro deja fuera los nulos.
        builder.HasIndex(f => f.Ncf)
            .IsUnique()
            .HasFilter("[Ncf] IS NOT NULL");

        // El reporte de ventas del dia y el cierre de caja filtran por fecha y estado.
        builder.HasIndex(f => new { f.Fecha, f.Estado });
        builder.HasIndex(f => new { f.ClienteId, f.Fecha });

        builder.HasOne(f => f.Cliente)
            .WithMany()
            .HasForeignKey(f => f.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Lineas)
            .WithOne(l => l.Factura)
            .HasForeignKey(l => l.FacturaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(f => f.EstaAnulada);
        builder.Ignore(f => f.Margen);
        builder.Ignore(f => f.MargenPorcentaje);
    }
}

public class LineaFacturaConfiguration : IEntityTypeConfiguration<LineaFactura>
{
    public void Configure(EntityTypeBuilder<LineaFactura> builder)
    {
        builder.ToTable("LineasFactura");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Descripcion).IsRequired().HasMaxLength(200);

        builder.Property(l => l.Cantidad).HasPrecision(18, 3);
        builder.Property(l => l.PrecioUnitario).HasPrecision(18, 4);
        builder.Property(l => l.CostoUnitario).HasPrecision(18, 4);
        builder.Property(l => l.TasaItbis).HasPrecision(5, 4);

        builder.Property(l => l.CreadoPor).HasMaxLength(450);
        builder.Property(l => l.ModificadoPor).HasMaxLength(450);

        builder.HasOne(l => l.Producto)
            .WithMany()
            .HasForeignKey(l => l.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // El reporte de rentabilidad por producto agrupa por aqui.
        builder.HasIndex(l => l.ProductoId);

        builder.Ignore(l => l.Subtotal);
        builder.Ignore(l => l.Itbis);
        builder.Ignore(l => l.Total);
        builder.Ignore(l => l.CostoTotal);
        builder.Ignore(l => l.Margen);
    }
}
