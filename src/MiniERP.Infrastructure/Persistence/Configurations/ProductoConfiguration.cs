using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Productos");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Codigo)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(p => p.CodigoBarras)
            .HasMaxLength(50);

        builder.Property(p => p.Descripcion)
            .IsRequired()
            .HasMaxLength(200);

        // Cuatro decimales en dinero: el redondeo se hace al totalizar, no al almacenar.
        // Un producto a 3.3333 por libra no debe perder precision en la base.
        builder.Property(p => p.Costo).HasPrecision(18, 4);
        builder.Property(p => p.PrecioVenta).HasPrecision(18, 4);

        builder.Property(p => p.TasaItbis).HasPrecision(5, 4);

        // Tres decimales en cantidad, que es lo que exige la venta por peso.
        builder.Property(p => p.Existencia).HasPrecision(18, 3);
        builder.Property(p => p.ExistenciaMinima).HasPrecision(18, 3);

        builder.Property(p => p.CreadoPor).HasMaxLength(450);
        builder.Property(p => p.ModificadoPor).HasMaxLength(450);

        builder.HasIndex(p => p.Codigo).IsUnique();

        // El codigo de barras es unico solo entre los que lo tienen: los productos a
        // granel no traen, y un filtro parcial evita que varios NULL choquen entre si.
        builder.HasIndex(p => p.CodigoBarras)
            .IsUnique()
            .HasFilter("[CodigoBarras] IS NOT NULL");

        builder.HasIndex(p => p.Descripcion);

        builder.HasOne(p => p.Categoria)
            .WithMany(c => c.Productos)
            .HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.UnidadMedida)
            .WithMany(u => u.Productos)
            .HasForeignKey(p => p.UnidadMedidaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Propiedades calculadas del dominio: no tienen columna.
        builder.Ignore(p => p.PrecioConItbis);
        builder.Ignore(p => p.MargenUnitario);
        builder.Ignore(p => p.MargenPorcentaje);
        builder.Ignore(p => p.RequiereReabastecimiento);
        builder.Ignore(p => p.Agotado);
    }
}
