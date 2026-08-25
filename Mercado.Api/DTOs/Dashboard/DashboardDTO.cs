namespace Mercado.Api.DTOs.Dashboard;

public class DashboardDTO
{
    public decimal TotalVendidoHoje { get; set; }

    public int QuantidadeVendasHoje { get; set; }

    public decimal FaturamentoTotal { get; set; }

    public string ProdutoMaisVendido { get; set; } = string.Empty;

    public int ProdutosEstoqueBaixo { get; set; }
}