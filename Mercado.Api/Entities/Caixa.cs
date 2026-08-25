namespace Mercado.Api.Entities;

public class Caixa
{
    public int Id { get; set; }

    public DateTime DataAbertura { get; set; }

    public DateTime? DataFechamento { get; set; }

    public decimal ValorInicial { get; set; }

    public decimal? ValorFinal { get; set; }

    public bool Aberto { get; set; }

    public List<Venda> Vendas { get; set; } = [];
}