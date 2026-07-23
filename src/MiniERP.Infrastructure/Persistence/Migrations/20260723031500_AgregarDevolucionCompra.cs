using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiniERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarDevolucionCompra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DevolucionesCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompraId = table.Column<int>(type: "int", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Itbis = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceAnterior = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceResultante = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionesCompra_Compras_CompraId",
                        column: x => x.CompraId,
                        principalTable: "Compras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DevolucionesCompra_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LineasDevolucionCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevolucionCompraId = table.Column<int>(type: "int", nullable: false),
                    LineaCompraId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    CostoUnitario = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TasaItbis = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModificadoPor = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasDevolucionCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineasDevolucionCompra_DevolucionesCompra_DevolucionCompraId",
                        column: x => x.DevolucionCompraId,
                        principalTable: "DevolucionesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LineasDevolucionCompra_LineasCompra_LineaCompraId",
                        column: x => x.LineaCompraId,
                        principalTable: "LineasCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LineasDevolucionCompra_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_CompraId_Estado",
                table: "DevolucionesCompra",
                columns: new[] { "CompraId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_Estado_Fecha",
                table: "DevolucionesCompra",
                columns: new[] { "Estado", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_Numero",
                table: "DevolucionesCompra",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionesCompra_ProveedorId_Fecha",
                table: "DevolucionesCompra",
                columns: new[] { "ProveedorId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_LineasDevolucionCompra_DevolucionCompraId",
                table: "LineasDevolucionCompra",
                column: "DevolucionCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasDevolucionCompra_LineaCompraId",
                table: "LineasDevolucionCompra",
                column: "LineaCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_LineasDevolucionCompra_ProductoId",
                table: "LineasDevolucionCompra",
                column: "ProductoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LineasDevolucionCompra");

            migrationBuilder.DropTable(
                name: "DevolucionesCompra");
        }
    }
}
