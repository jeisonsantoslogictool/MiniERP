using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Cantidad).HasPrecision(18, 3);
        builder.Property(m => m.ExistenciaAnterior).HasPrecision(18, 3);
        builder.Property(m => m.ExistenciaResultante).HasPrecision(18, 3);
        builder.Property(m => m.CostoUnitario).HasPrecision(18, 4);

        builder.Property(m => m.Tipo)
            .HasConversion<int>();

        builder.Property(m => m.Motivo).HasMaxLength(250);
        builder.Property(m => m.ReferenciaTipo).HasMaxLength(20);
        builder.Property(m => m.UsuarioId).HasMaxLength(450);
        builder.Property(m => m.CreadoPor).HasMaxLength(450);
        builder.Property(m => m.ModificadoPor).HasMaxLength(450);

        builder.HasOne(m => m.Producto)
            .WithMany(p => p.Movimientos)
            .HasForeignKey(m => m.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        // El kardex de un producto se consulta siempre del mas reciente hacia atras.
        builder.HasIndex(m => new { m.ProductoId, m.Fecha });

        // Los reportes de merma y de ajustes filtran por tipo dentro de un rango de fechas.
        builder.HasIndex(m => new { m.Tipo, m.Fecha });

        builder.HasIndex(m => new { m.ReferenciaTipo, m.ReferenciaId });

        builder.Ignore(m => m.EsEntrada);
        builder.Ignore(m => m.ValorCosto);
        builder.Ignore(m => m.RequiereMotivo);
    }
}
