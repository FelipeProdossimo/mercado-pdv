namespace Mercado.Api.DTOs.Dashboard;

public class ProdutoMaisVendidoDTO
{
    public string Produto { get; set; } = string.Empty;

    public int QuantidadeVendida { get; set; }
}