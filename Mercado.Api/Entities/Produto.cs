namespace Mercado.Api.Entities;

public class Produto
{
    public int Id { get; set; }

    public bool Ativo { get; set; } = true;

    public string Descricao { get; set; } = string.Empty;

    public decimal Valor { get; set; }

    public int Estoque { get; set; }

    public string CodigoBarras { get; set; } = string.Empty;

    public string? ImagemUrl { get; set; }

    // Campos fiscais exigidos pela NFC-e. Ficam nulos/genéricos em produtos
    // cadastrados antes de existir emissão de nota — precisam ser
    // preenchidos produto a produto (geralmente com apoio de um contador)
    // antes de qualquer emissão real.
    public string? Ncm { get; set; }

    public string? Cfop { get; set; }

    public string? Cest { get; set; }

    // Tabela de unidades da NF-e (ex.: "UN", "KG", "CX").
    public string UnidadeMedida { get; set; } = "UN";

    // Código de origem da mercadoria da NF-e (0 = nacional, 1 = importada
    // direta, etc.).
    public int OrigemMercadoria { get; set; } = 0;
}