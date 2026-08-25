using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mercado.Api.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCamposFiscaisNfce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChaveAcessoNfce",
                table: "Vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpfCnpjCliente",
                table: "Vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkDanfeNfce",
                table: "Vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRejeicaoNfce",
                table: "Vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaNfce",
                table: "Vendas",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusEmissaoNfce",
                table: "Vendas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Cest",
                table: "Produtos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cfop",
                table: "Produtos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ncm",
                table: "Produtos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OrigemMercadoria",
                table: "Produtos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UnidadeMedida",
                table: "Produtos",
                type: "text",
                nullable: false,
                defaultValue: "UN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChaveAcessoNfce",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "CpfCnpjCliente",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "LinkDanfeNfce",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "MotivoRejeicaoNfce",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "ReferenciaNfce",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "StatusEmissaoNfce",
                table: "Vendas");

            migrationBuilder.DropColumn(
                name: "Cest",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Cfop",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "Ncm",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "OrigemMercadoria",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "UnidadeMedida",
                table: "Produtos");
        }
    }
}
