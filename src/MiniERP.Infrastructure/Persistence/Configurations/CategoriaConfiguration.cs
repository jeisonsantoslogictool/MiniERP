using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("Categorias");

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
