using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Desafio.Credito.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvolucaoContrato",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Dia = table.Column<int>(type: "int", nullable: false),
                    Prestacao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JurosPeriodo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amortizacao = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SaldoAposPagar = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataProcessamento = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvolucaoContrato", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvolucaoContrato");
        }
    }
}
