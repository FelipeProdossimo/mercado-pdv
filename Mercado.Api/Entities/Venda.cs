using Mercado.Api.Enums;

namespace Mercado.Api.Entities;

public class Venda
{
    public int Id { get; set; }

    public DateTime DataVenda { get; set; }

    public decimal ValorTotal { get; set; }

    public List<ItemVenda> Itens { get; set; } = [];

    public FormaPagamentoEnum FormaPagamento { get; set; }

    // Nulo apenas em vendas registradas antes do vínculo com caixa/usuário
    // existir. Vendas novas sempre recebem os dois valores (VendasController).
    public int? CaixaId { get; set; }

    public Caixa? Caixa { get; set; }

    public int? UsuarioId { get; set; }

    public Usuario? Usuario { get; set; }

    // Dinheiro/cartão continuam simulados e já nascem Aprovado. PIX real
    // nasce Pendente e só vira Aprovado quando o Mercado Pago confirmar.
    public StatusPagamentoEnum StatusPagamento { get; set; } = StatusPagamentoEnum.Aprovado;

    // Id do pagamento no Mercado Pago (preenchido apenas para vendas em PIX).
    public string? PagamentoExternoId { get; set; }

    // CPF/CNPJ do cliente para constar na nota (opcional — NFC-e permite
    // emitir sem identificar o consumidor).
    public string? CpfCnpjCliente { get; set; }

    public StatusEmissaoNfceEnum StatusEmissaoNfce { get; set; } =
        StatusEmissaoNfceEnum.NaoEmitida;

    // Referência que a Mercado.Api gera e envia à Focus NFe (não é a chave
    // de acesso da nota) — usada para consultar o status depois.
    public string? ReferenciaNfce { get; set; }

    // Chave de acesso de 44 dígitos, só existe após autorização.
    public string? ChaveAcessoNfce { get; set; }

    public string? LinkDanfeNfce { get; set; }

    // Motivo devolvido pela SEFAZ/Focus NFe quando StatusEmissaoNfce fica
    // Rejeitada — mostrado pro caixa decidir o que corrigir.
    public string? MotivoRejeicaoNfce { get; set; }
}