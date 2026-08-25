namespace Mercado.Api.DTOs.Venda;

public class PagamentoPixRespostaDTO
{
    public int VendaId { get; set; }

    public decimal ValorTotal { get; set; }

    public string QrCode { get; set; } = string.Empty;

    public string QrCodeBase64 { get; set; } = string.Empty;

    public DateTime ExpiraEm { get; set; }
}
