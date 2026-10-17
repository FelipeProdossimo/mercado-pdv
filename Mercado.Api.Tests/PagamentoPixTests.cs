using System.Net;
using System.Net.Http.Json;
using Mercado.Api.Enums;
using Mercado.Api.Tests.Infra;

namespace Mercado.Api.Tests;

[Collection(ApiCollection.Nome)]
public class PagamentoPixTests(MercadoApiFactory api) : TesteDeIntegracao(api)
{
    private async Task<int> IniciarPix(int produtoId, int quantidade)
    {
        var resposta = await Client.PostAsJsonAsync("/api/vendas/pix/iniciar", new
        {
            itens = Cenario.Itens((produtoId, quantidade))
        });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);

        var cobranca = await resposta.Content.ReadFromJsonAsync<CobrancaPix>();
        Assert.False(string.IsNullOrEmpty(cobranca!.QrCode));

        return cobranca.VendaId;
    }

    private async Task<StatusPagamentoEnum> ConsultarStatus(int vendaId)
    {
        var resposta = await Client.GetFromJsonAsync<StatusPix>($"/api/vendas/pix/{vendaId}/status");
        return resposta!.Status;
    }

    [Fact]
    public async Task Gerar_o_QR_Code_nao_baixa_o_estoque()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();

        var vendaId = await IniciarPix(arroz.Id, 3);

        Assert.Equal(StatusPagamentoEnum.Pendente, await ConsultarStatus(vendaId));
        Assert.Equal(10, await Api.EstoqueAtual(arroz.Id));
    }

    [Fact]
    public async Task Pix_confirmado_aprova_a_venda_e_baixa_o_estoque()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();
        var vendaId = await IniciarPix(arroz.Id, 3);

        Api.Pix.StatusAtual = "processed";

        Assert.Equal(StatusPagamentoEnum.Aprovado, await ConsultarStatus(vendaId));
        Assert.Equal(7, await Api.EstoqueAtual(arroz.Id));
    }

    [Fact]
    public async Task Consultar_de_novo_nao_baixa_o_estoque_duas_vezes()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();
        var vendaId = await IniciarPix(arroz.Id, 3);
        Api.Pix.StatusAtual = "processed";

        await ConsultarStatus(vendaId);
        await ConsultarStatus(vendaId);
        await ConsultarStatus(vendaId);

        Assert.Equal(7, await Api.EstoqueAtual(arroz.Id));
        Assert.Equal(1, Api.Pix.Consultas); // depois de aprovado, não chama mais o Mercado Pago
    }

    [Fact]
    public async Task Pix_expirado_cancela_a_venda_sem_mexer_no_estoque()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();
        var vendaId = await IniciarPix(arroz.Id, 3);

        Api.Pix.StatusAtual = "expired";

        Assert.Equal(StatusPagamentoEnum.Cancelado, await ConsultarStatus(vendaId));
        Assert.Equal(10, await Api.EstoqueAtual(arroz.Id));
    }

    [Fact]
    public async Task Pix_cancelado_pelo_operador_nao_pode_ser_cancelado_de_novo()
    {
        var arroz = await Api.CriarProduto(estoque: 10);
        await Client.AbrirCaixa();
        var vendaId = await IniciarPix(arroz.Id, 1);

        var primeiro = await Client.PostAsync($"/api/vendas/pix/{vendaId}/cancelar", null);
        var segundo = await Client.PostAsync($"/api/vendas/pix/{vendaId}/cancelar", null);

        Assert.Equal(HttpStatusCode.OK, primeiro.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, segundo.StatusCode);
    }

    private record CobrancaPix(int VendaId, decimal ValorTotal, string QrCode);

    private record StatusPix(StatusPagamentoEnum Status);
}
