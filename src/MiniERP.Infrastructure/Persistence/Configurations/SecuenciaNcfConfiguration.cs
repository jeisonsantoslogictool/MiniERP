using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Ventas;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class SecuenciaNcfConfiguration : IEntityTypeConfiguration<SecuenciaNcf>
{
    public void Configure(EntityTypeBuilder<SecuenciaNcf> builder)
    {
        builder.ToTable("SecuenciasNcf");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.TipoComprobante).HasConversion<int>();

        builder.Property(s => s.Prefijo).IsRequired().HasMaxLength(3);

        builder.Property(s => s.CreadoPor).HasMaxLength(450);
        builder.Property(s => s.ModificadoPor).HasMaxLength(450);

        // La asignacion busca por tipo entre las activas: es la consulta que corre en
        // cada venta, con la caja esperando.
        builder.HasIndex(s => new { s.TipoComprobante, s.Activa });

        builder.HasIndex(s => s.Prefijo);

        builder.Ignore(s => s.Disponibles);
        builder.Ignore(s => s.Agotada);
        builder.Ignore(s => s.PorAgotarse);
        builder.Ignore(s => s.LargoCorrelativo);
        builder.Ignore(s => s.EsElectronica);
    }
}
