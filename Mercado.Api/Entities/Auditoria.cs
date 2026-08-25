namespace Mercado.Api.Entities;

public class Auditoria
{
    public int Id { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Acao { get; set; } = string.Empty;

    public DateTime Data { get; set; }

    public string Detalhes { get; set; } = string.Empty;
}