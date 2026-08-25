namespace Mercado.Api.DTOs.Venda;

public class ItemVendaRespostaDTO
{
    public int ProdutoId { get; set; }

    public string DescricaoProduto { get; set; } = string.Empty;

    public int Quantidade { get; set; }

    public decimal ValorUnitario { get; set; }

    public decimal Total { get; set; }
}