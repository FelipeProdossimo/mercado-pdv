using System.ComponentModel.DataAnnotations;

namespace Mercado.Api.DTOs.Venda;

public class ItemVendaDTO
{
    public int ProdutoId { get; set; }

    // Sem esta validação, uma quantidade negativa passava pelo
    // "WHERE Estoque >= quantidade" e AUMENTAVA o estoque, gerando venda
    // com valor negativo.
    [Range(1, int.MaxValue, ErrorMessage = "A quantidade deve ser maior que zero")]
    public int Quantidade { get; set; }
}