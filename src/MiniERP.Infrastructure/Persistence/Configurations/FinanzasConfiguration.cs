using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class CategoriaEgresoConfiguration : IEntityTypeConfiguration<CategoriaEgreso>
{
    public void Configure(EntityTypeBuilder<CategoriaEgreso> builder)
    {
        builder.ToTable("CategoriasEgreso");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Nombre)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(c => c.Descripcion)
            .HasMaxLength(250);

        builder.Property(c => c.CreadoPor).HasMaxLength(450);
        builder.Property(c => c.ModificadoPor).HasMaxLength(450);

        builder.HasIndex(c => c.Nombre).IsUnique();
    }
}

public class EgresoConfiguration : IEntityTypeConfiguration<Egreso>
{
    public void Configure(EntityTypeBuilder<Egreso> builder)
    {
        builder.ToTable("Egresos");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Monto).HasPrecision(18, 2);

        builder.Property(e => e.Descripcion).HasMaxLength(300);
        builder.Property(e => e.UsuarioId).HasMaxLength(450);
        builder.Property(e => e.CreadoPor).HasMaxLength(450);
        builder.Property(e => e.ModificadoPor).HasMaxLength(450);

        builder.HasOne(e => e.CategoriaEgreso)
            .WithMany(c => c.Egresos)
            .HasForeignKey(e => e.CategoriaEgresoId)
            .OnDelete(DeleteBehavior.Restrict);

        // Los reportes de egresos filtran por rango de fechas y agrupan por categoria.
        builder.HasIndex(e => e.Fecha);
        builder.HasIndex(e => new { e.CategoriaEgresoId, e.Fecha });
    }
}
