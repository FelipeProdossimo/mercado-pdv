using Mercado.Api.DTOs.Venda;
using Mercado.Api.Enums;

public class CriarVendaDTO
{
    public FormaPagamentoEnum FormaPagamento { get; set; }

    public List<ItemVendaDTO> Itens { get; set; } = [];
}