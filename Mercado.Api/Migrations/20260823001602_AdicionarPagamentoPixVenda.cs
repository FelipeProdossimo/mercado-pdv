using Mercado.Api.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercado.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPagamentoPixVenda : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PagamentoExternoId",
                table: "Vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusPagamento",
                table: "Vendas",
                type: "integer",
                nullable: false,
                // Vendas registradas antes desta coluna existir eram todas
                // simuladas (dinheiro/cartão) e já concluídas.
                defaultValue: (int)StatusPagamentoEnum.Aprovado);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PagamentoExternoId",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "StatusPagamento",
                table: "Vendas");
        }
    }
}
