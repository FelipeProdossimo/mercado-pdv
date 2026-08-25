namespace Mercado.Api.DTOs.Produto;

public class ProdutoCriarDTO
{
    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public int Estoque { get; set; }

    public string CodigoBarras { get; set; } = string.Empty;
}