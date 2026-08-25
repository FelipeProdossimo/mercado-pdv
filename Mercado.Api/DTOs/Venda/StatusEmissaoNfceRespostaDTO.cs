using Mercado.Api.Enums;

namespace Mercado.Api.DTOs.Venda;

public class StatusEmissaoNfceRespostaDTO
{
    public StatusEmissaoNfceEnum Status { get; set; }

    public string? ChaveAcesso { get; set; }

    public string? LinkDanfe { get; set; }

    public string? MotivoRejeicao { get; set; }
}
