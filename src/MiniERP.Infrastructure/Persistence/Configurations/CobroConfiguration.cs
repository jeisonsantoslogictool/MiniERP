using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Clientes;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class CobroConfiguration : IEntityTypeConfiguration<Cobro>
{
    public void Configure(EntityTypeBuilder<Cobro> builder)
    {
        builder.ToTable("Cobros");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Monto)
            .HasPrecision(18, 2);

        builder.Property(c => c.BalanceAnterior)
            .HasPrecision(18, 2);

        builder.Property(c => c.BalanceResultante)
            .HasPrecision(18, 2);

        builder.Property(c => c.Observacion)
            .HasMaxLength(250);

        builder.Property(c => c.UsuarioId)
            .HasMaxLength(450);

        builder.Property(c => c.CreadoPor)
            .HasMaxLength(100);

        builder.Property(c => c.ModificadoPor)
            .HasMaxLength(100);

        builder.HasOne(c => c.Cliente)
            .WithMany(cl => cl.Cobros)
            .HasForeignKey(c => c.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice para agilizar listados por cliente y ordenados por fecha
        builder.HasIndex(c => new { c.ClienteId, c.Fecha });
    }
}
