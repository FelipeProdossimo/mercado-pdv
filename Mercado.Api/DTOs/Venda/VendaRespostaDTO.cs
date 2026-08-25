namespace Mercado.Api.DTOs.Venda;

public class VendaRespostaDTO
{
    public int Id { get; set; }

    public DateTime DataVenda { get; set; }

    public decimal ValorTotal { get; set; }

    public List<ItemVendaRespostaDTO> Itens { get; set; } = [];
}