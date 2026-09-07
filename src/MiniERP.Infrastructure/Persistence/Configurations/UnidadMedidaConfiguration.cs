using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Configurations;

public class UnidadMedidaConfiguration : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> builder)
    {
        builder.ToTable("UnidadesMedida");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Codigo)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(u => u.Nombre)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.CreadoPor).HasMaxLength(450);
        builder.Property(u => u.ModificadoPor).HasMaxLength(450);

        builder.HasIndex(u => u.Codigo).IsUnique();

        // Datos semilla: el catalogo minimo con que un minimarket puede operar el
        // primer dia, cubriendo venta por unidad y por peso.
        //
        // La fecha va fija a proposito. EntidadBase inicializa FechaCreacion con
        // RelojSimulado.UtcNow, y un valor dinamico dentro de HasData haria que el modelo
        // cambiara en cada compilacion y las migraciones nunca cerraran.
        var semilla = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        builder.HasData(
            new UnidadMedida { Id = 1, Codigo = "UND", Nombre = "Unidad",    PermiteDecimales = false, CantidadDecimales = 0, Activo = true, FechaCreacion = semilla },
            new UnidadMedida { Id = 2, Codigo = "LB",  Nombre = "Libra",     PermiteDecimales = true,  CantidadDecimales = 3, Activo = true, FechaCreacion = semilla },
            new UnidadMedida { Id = 3, Codigo = "KG",  Nombre = "Kilogramo", PermiteDecimales = true,  CantidadDecimales = 3, Activo = true, FechaCreacion = semilla },
            new UnidadMedida { Id = 4, Codigo = "GAL", Nombre = "Galon",     PermiteDecimales = true,  CantidadDecimales = 2, Activo = true, FechaCreacion = semilla },
            new UnidadMedida { Id = 5, Codigo = "LT",  Nombre = "Litro",     PermiteDecimales = true,  CantidadDecimales = 2, Activo = true, FechaCreacion = semilla },
            new UnidadMedida { Id = 6, Codigo = "CAJ", Nombre = "Caja",      PermiteDecimales = false, CantidadDecimales = 0, Activo = true, FechaCreacion = semilla },
            new UnidadMedida { Id = 7, Codigo = "PAQ", Nombre = "Paquete",   PermiteDecimales = false, CantidadDecimales = 0, Activo = true, FechaCreacion = semilla });
    }
}
