using System.Net;
using System.Net.Http.Json;
using Mercado.Api.Enums;
using Mercado.Api.Tests.Infra;

namespace Mercado.Api.Tests;

[Collection(ApiCollection.Nome)]
public class CaixaTests(MercadoApiFactory api) : TesteDeIntegracao(api)
{
    [Fact]
    public async Task Nao_abre_dois_caixas_ao_mesmo_tempo()
    {
        await Client.AbrirCaixa();

        var segundo = await Client.PostAsJsonAsync("/api/caixa/abrir", new { valorInicial = 50m });

        Assert.Equal(HttpStatusCode.BadRequest, segundo.StatusCode);
    }

    [Fact]
    public async Task Fechamento_soma_apenas_vendas_aprovadas()
    {
        var arroz = await Api.CriarProduto(valor: 10m, estoque: 50);
        await Client.AbrirCaixa(valorInicial: 100m);

        // Aprovada em dinheiro: R$ 30
        await Client.PostAsJsonAsync("/api/vendas", new
        {
            formaPagamento = FormaPagamentoEnum.Dinheiro,
            itens = Cenario.Itens((arroz.Id, 3))
        });

        // PIX que ficou pendente: R$ 50, não pode entrar no fechamento
        await Client.PostAsJsonAsync("/api/vendas/pix/iniciar", new
        {
            itens = Cenario.Itens((arroz.Id, 5))
        });

        var resposta = await Client.PostAsync("/api/caixa/fechar", null);
        var fechamento = await resposta.Content.ReadFromJsonAsync<Fechamento>();

        Assert.Equal(30m, fechamento!.TotalVendas);
        Assert.Equal(130m, fechamento.ValorFinal);
    }

    private record Fechamento(decimal ValorInicial, decimal TotalVendas, decimal ValorFinal);
}
