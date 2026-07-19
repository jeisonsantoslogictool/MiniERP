using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Compras;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> builder)
    {
        builder.ToTable("Pagos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Monto)
            .HasPrecision(18, 2);

        builder.Property(p => p.BalanceAnterior)
            .HasPrecision(18, 2);

        builder.Property(p => p.BalanceResultante)
            .HasPrecision(18, 2);

        builder.Property(p => p.Observacion)
            .HasMaxLength(250);

        builder.Property(p => p.UsuarioId)
            .HasMaxLength(450);

        builder.Property(p => p.CreadoPor)
            .HasMaxLength(100);

        builder.Property(p => p.ModificadoPor)
            .HasMaxLength(100);

        builder.HasOne(p => p.Proveedor)
            .WithMany(pr => pr.Pagos)
            .HasForeignKey(p => p.ProveedorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.ProveedorId, p.Fecha });
    }
}
