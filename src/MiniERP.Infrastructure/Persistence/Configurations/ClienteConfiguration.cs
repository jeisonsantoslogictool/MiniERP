using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Clientes;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Codigo)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.TipoDocumento).HasConversion<int>();
        builder.Property(c => c.TipoComprobante).HasConversion<int>();

        builder.Property(c => c.NumeroDocumento).HasMaxLength(20);
        builder.Property(c => c.Telefono).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(150);
        builder.Property(c => c.Direccion).HasMaxLength(250);

        builder.Property(c => c.LimiteCredito).HasPrecision(18, 2);
        builder.Property(c => c.BalanceActual).HasPrecision(18, 2);

        builder.Property(c => c.CreadoPor).HasMaxLength(450);
        builder.Property(c => c.ModificadoPor).HasMaxLength(450);

        builder.HasIndex(c => c.Codigo).IsUnique();
        builder.HasIndex(c => c.Nombre);

        // Un mismo RNC no debe entrar dos veces, pero el cliente de contado no trae
        // documento: el filtro deja fuera los nulos para que puedan repetirse.
        builder.HasIndex(c => c.NumeroDocumento)
            .IsUnique()
            .HasFilter("[NumeroDocumento] IS NOT NULL");

        builder.Ignore(c => c.TieneCredito);
        builder.Ignore(c => c.CreditoDisponible);
        builder.Ignore(c => c.ExcedeLimite);
        builder.Ignore(c => c.ComprobanteEstaSustentado);
    }
}
