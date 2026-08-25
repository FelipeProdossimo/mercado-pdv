namespace Mercado.Api.DTOs.Venda;

public class EmitirNfceDTO
{
    // Opcional — NFC-e permite emitir sem identificar o consumidor.
    public string? CpfCnpjCliente { get; set; }
}
