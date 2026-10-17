using System.Net.Http.Json;
using Mercado.Api.Entities;
using Mercado.Api.Enums;
using Mercado.Api.Tests.Infra;

namespace Mercado.Api.Tests;

[Collection(ApiCollection.Nome)]
public class DashboardTests(MercadoApiFactory api) : TesteDeIntegracao(api)
{
    private async Task CriarVenda(DateTime data, decimal valor, StatusPagamentoEnum status, int produtoId = 0)
    {
        await Api.ExecutarNoBanco(async context =>
        {
            var venda = new Venda
            {
                DataVenda = DateTime.SpecifyKind(data, DateTimeKind.Utc),
                ValorTotal = valor,
                FormaPagamento = FormaPagamentoEnum.Pix,
                StatusPagamento = status
            };

            if (produtoId > 0)
            {
                venda.Itens.Add(new ItemVenda
                {
                    ProdutoId = produtoId,
                    Quantidade = 1,
                    ValorUnitario = valor,
                    Total = valor
                });
            }

            context.Vendas.Add(venda);
            await context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Grafico_mensal_vem_em_ordem_cronologica()
    {
        // Como texto, "10/2026" vem antes de "5/2026"; o gráfico precisa
        // seguir o calendário.
        await CriarVenda(new DateTime(2026, 10, 1), 10m, StatusPagamentoEnum.Aprovado);
        await CriarVenda(new DateTime(2026, 5, 1), 20m, StatusPagamentoEnum.Aprovado);
        await CriarVenda(new DateTime(2025, 12, 1), 30m, StatusPagamentoEnum.Aprovado);

        var grafico = await Client.GetFromJsonAsync<List<PontoGrafico>>("/api/dashboard/grafico-vendas");

        Assert.Equal(["12/2025", "5/2026", "10/2026"], grafico!.Select(x => x.Mes));
    }

    [Fact]
    public async Task Dashboard_ignora_vendas_nao_aprovadas()
    {
        var arroz = await Api.CriarProduto("Arroz 5kg");
        var feijao = await Api.CriarProduto("Feijão 1kg");
        var hoje = DateTime.UtcNow;

        await CriarVenda(hoje, 30m, StatusPagamentoEnum.Aprovado, arroz.Id);
        await CriarVenda(hoje, 500m, StatusPagamentoEnum.Pendente, feijao.Id);
        await CriarVenda(hoje, 500m, StatusPagamentoEnum.Cancelado, feijao.Id);
        await CriarVenda(hoje, 500m, StatusPagamentoEnum.Cancelado, feijao.Id);

        var resumo = await Client.GetFromJsonAsync<Resumo>("/api/dashboard");
        var grafico = await Client.GetFromJsonAsync<List<PontoGrafico>>("/api/dashboard/grafico-vendas");

        Assert.Equal(30m, resumo!.TotalVendidoHoje);
        Assert.Equal(1, resumo.QuantidadeVendasHoje);
        Assert.Equal("Arroz 5kg", resumo.ProdutoMaisVendido);
        Assert.Equal(30m, grafico!.Single().Total);
    }

    private record PontoGrafico(string Mes, decimal Total);

    private record Resumo(decimal TotalVendidoHoje, int QuantidadeVendasHoje, string ProdutoMaisVendido);
}
