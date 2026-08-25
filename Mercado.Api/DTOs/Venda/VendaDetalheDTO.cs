using Mercado.Api.Enums;

namespace Mercado.Api.DTOs.Venda;

public class VendaDetalheDTO
{
    public int Id { get; set; }

    public DateTime DataVenda { get; set; }

    public decimal ValorTotal { get; set; }

    public StatusPagamentoEnum StatusPagamento { get; set; }

    public List<ItemVendaRespostaDTO> Itens { get; set; } = [];
}