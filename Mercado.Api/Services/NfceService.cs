using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mercado.Api.Entities;

namespace Mercado.Api.Services;

public record ResultadoEmissaoNfce(
    string Status,
    string? ChaveAcesso,
    string? LinkDanfe,
    string? MotivoRejeicao);

// Integração com a Focus NFe (https://focusnfe.com.br) para emissão de
// NFC-e. Foco no ambiente de homologação: a Mercado.Api ainda não tem CNPJ
// próprio, então tudo aqui roda com a empresa fictícia cadastrada no painel
// de testes da Focus NFe (ver README para o passo a passo de cadastro).
public class NfceService
{
    private readonly HttpClient _http;
    private readonly EmpresaFiscalConfig _empresa;

    public NfceService(HttpClient http, IConfiguration configuration)
    {
        _http = http;

        _empresa = new EmpresaFiscalConfig
        {
            Cnpj = configuration["Nfce:EmpresaCnpj"] ?? string.Empty,
            RazaoSocial = configuration["Nfce:EmpresaRazaoSocial"] ?? string.Empty,
            InscricaoEstadual = configuration["Nfce:EmpresaInscricaoEstadual"] ?? string.Empty,
            RegimeTributario = configuration["Nfce:EmpresaRegimeTributario"] ?? "1" // 1 = Simples Nacional
        };

        // Token de homologação da Focus NFe, obtido no painel deles depois
        // de cadastrar a empresa de teste (ver README). A API deles usa
        // Basic Auth com o token como usuário e senha em branco.
        var token = configuration["Nfce:FocusNfeToken"];

        if (!string.IsNullOrWhiteSpace(token))
        {
            var credenciais = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{token}:"));

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credenciais);
        }

        _http.BaseAddress = new Uri("https://homologacao.focusnfe.com.br");
    }

    public async Task<ResultadoEmissaoNfce> EmitirNfce(Venda venda, string referencia)
    {
        var payload = new
        {
            natureza_operacao = "Venda",
            data_emissao = venda.DataVenda.ToString("yyyy-MM-ddTHH:mm:sszzz"),
            presenca_comprador = "1", // operação presencial
            modalidade_frete = "9", // sem frete

            cnpj_emitente = _empresa.Cnpj,

            cpf_cnpj_consumidor = string.IsNullOrWhiteSpace(venda.CpfCnpjCliente)
                ? null
                : venda.CpfCnpjCliente,

            items = venda.Itens.Select((item, indice) => new
            {
                numero_item = indice + 1,
                codigo_produto = item.ProdutoId.ToString(),
                descricao = item.Produto.Descricao,
                cfop = item.Produto.Cfop,
                ncm = item.Produto.Ncm,
                cest = item.Produto.Cest,
                unidade_comercial = item.Produto.UnidadeMedida,
                quantidade_comercial = item.Quantidade,
                valor_unitario_comercial = item.ValorUnitario,
                valor_bruto = item.Total,
                unidade_tributavel = item.Produto.UnidadeMedida,
                quantidade_tributavel = item.Quantidade,
                valor_unitario_tributavel = item.ValorUnitario,
                icms_origem = item.Produto.OrigemMercadoria,

                // Simples Nacional: CSOSN 102 (sem permissão de crédito) é
                // um ponto de partida comum, mas precisa ser validado pelo
                // contador conforme o produto/regime real.
                icms_situacao_tributaria = "102"
            }).ToList()
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var resposta = await _http.PostAsync(
            $"/v2/nfce?ref={referencia}",
            new StringContent(json, Encoding.UTF8, "application/json"));

        var corpo = await resposta.Content.ReadAsStringAsync();

        return InterpretarResposta(corpo);
    }

    public async Task<ResultadoEmissaoNfce> ConsultarStatus(string referencia)
    {
        var resposta = await _http.GetAsync($"/v2/nfce/{referencia}");

        var corpo = await resposta.Content.ReadAsStringAsync();

        return InterpretarResposta(corpo);
    }

    private static ResultadoEmissaoNfce InterpretarResposta(string corpoJson)
    {
        using var documento = JsonDocument.Parse(corpoJson);

        var raiz = documento.RootElement;

        var status = raiz.TryGetProperty("status", out var statusProp)
            ? statusProp.GetString() ?? "erro"
            : "erro";

        string? chaveAcesso = raiz.TryGetProperty("chave_nfe", out var chaveProp)
            ? chaveProp.GetString()
            : null;

        string? linkDanfe = raiz.TryGetProperty("caminho_danfe", out var danfeProp)
            ? danfeProp.GetString()
            : null;

        string? motivoRejeicao = raiz.TryGetProperty("mensagem_sefaz", out var motivoProp)
            ? motivoProp.GetString()
            : null;

        return new ResultadoEmissaoNfce(status, chaveAcesso, linkDanfe, motivoRejeicao);
    }

    private class EmpresaFiscalConfig
    {
        public string Cnpj { get; set; } = string.Empty;
        public string RazaoSocial { get; set; } = string.Empty;
        public string InscricaoEstadual { get; set; } = string.Empty;
        public string RegimeTributario { get; set; } = string.Empty;
    }
}
