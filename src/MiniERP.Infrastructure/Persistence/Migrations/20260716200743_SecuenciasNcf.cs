using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecuenciasNcf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecuenciasNcf",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoComprobante = table.Column<int>(type: "int", nullable: false),
                    Prefijo = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Desde = table.Column<long>(type: "bigint", nullable: false),
                    Hasta = table.Column<long>(type: "bigint", nullable: false),
                    Actual = table.Column<long>(type: "bigint", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuenciasNcf", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecuenciasNcf_Prefijo",
                table: "SecuenciasNcf",
                column: "Prefijo");

            migrationBuilder.CreateIndex(
                name: "IX_SecuenciasNcf_TipoComprobante_Activa",
                table: "SecuenciasNcf",
                columns: new[] { "TipoComprobante", "Activa" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecuenciasNcf");
        }
    }
}
