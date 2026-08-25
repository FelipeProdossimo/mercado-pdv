using System.Security.Claims;
using Mercado.Api.Context;
using Mercado.Api.DTOs.Venda;
using Mercado.Api.Entities;
using Mercado.Api.Enums;
using Mercado.Api.Hubs;
using Mercado.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VendasController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificacaoHub> _hub;
    private readonly PagamentoPixService _pagamentoPixService;
    private readonly PagamentoCartaoService _pagamentoCartaoService;
    private readonly NfceService _nfceService;
    private readonly ILogger<VendasController> _logger;

    public VendasController(
    AppDbContext context,
    IHubContext<NotificacaoHub> hub,
    PagamentoPixService pagamentoPixService,
    PagamentoCartaoService pagamentoCartaoService,
    NfceService nfceService,
    ILogger<VendasController> logger)
    {
        _context = context;
        _hub = hub;
        _pagamentoPixService = pagamentoPixService;
        _pagamentoCartaoService = pagamentoCartaoService;
        _nfceService = nfceService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult> CriarVenda(CriarVendaDTO dto)
    {
        if (!dto.Itens.Any())
        {
            return BadRequest("Venda sem itens");
        }

        var caixa = await _context.Caixas
            .FirstOrDefaultAsync(x => x.Aberto);

        if (caixa == null)
        {
            return BadRequest(
                "Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");
        }

        var usuarioId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var produtoIds = dto.Itens
            .Select(x => x.ProdutoId)
            .Distinct()
            .ToList();

        var produtos = await _context.Produtos
            .Where(x => produtoIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        foreach (var itemDto in dto.Itens)
        {
            if (!produtos.ContainsKey(itemDto.ProdutoId))
            {
                return BadRequest(
                    $"Produto {itemDto.ProdutoId} não encontrado");
            }
        }

        var venda = new Venda
        {
            DataVenda = DateTime.UtcNow,
            FormaPagamento = dto.FormaPagamento,
            StatusPagamento = StatusPagamentoEnum.Aprovado,
            CaixaId = caixa.Id,
            UsuarioId = usuarioId
        };

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var erroEstoque = await MontarItensComBaixaDeEstoque(venda, dto.Itens, produtos);

        if (erroEstoque != null)
        {
            await transaction.RollbackAsync();

            return BadRequest(erroEstoque);
        }

        await _context.Vendas.AddAsync(venda);

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        await _hub.Clients.All.SendAsync(
            "VendaCriada",
            new
            {
                venda.Id,
                venda.ValorTotal,
                venda.DataVenda
            });

        var response = new VendaRespostaDTO
        {
            Id = venda.Id,
            DataVenda = venda.DataVenda,
            ValorTotal = venda.ValorTotal,

            Itens = venda.Itens.Select(i =>
                new ItemVendaRespostaDTO
                {
                    ProdutoId = i.ProdutoId,
                    Quantidade = i.Quantidade,
                    ValorUnitario = i.ValorUnitario,
                    Total = i.Total
                }).ToList()
        };

        return Ok(response);
    }

    [HttpPost("pix/iniciar")]
    public async Task<ActionResult> IniciarPagamentoPix(IniciarPagamentoPixDTO dto)
    {
        if (!dto.Itens.Any())
        {
            return BadRequest("Venda sem itens");
        }

        var caixa = await _context.Caixas
            .FirstOrDefaultAsync(x => x.Aberto);

        if (caixa == null)
        {
            return BadRequest(
                "Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");
        }

        var usuarioId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var produtoIds = dto.Itens
            .Select(x => x.ProdutoId)
            .Distinct()
            .ToList();

        var produtos = await _context.Produtos
            .Where(x => produtoIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        decimal valorTotal = 0;

        foreach (var itemDto in dto.Itens)
        {
            if (!produtos.TryGetValue(itemDto.ProdutoId, out var produto))
            {
                return BadRequest(
                    $"Produto {itemDto.ProdutoId} não encontrado");
            }

            if (produto.Estoque < itemDto.Quantidade)
            {
                return BadRequest(
                    $"Estoque insuficiente para {produto.Descricao}");
            }

            valorTotal += produto.Valor * itemDto.Quantidade;
        }

        // Estoque só é baixado quando o pagamento for confirmado (ver
        // ConsultarStatusPix) — aqui só validamos que existe disponibilidade.
        var venda = new Venda
        {
            DataVenda = DateTime.UtcNow,
            FormaPagamento = FormaPagamentoEnum.Pix,
            StatusPagamento = StatusPagamentoEnum.Pendente,
            ValorTotal = valorTotal,
            CaixaId = caixa.Id,
            UsuarioId = usuarioId,

            Itens = dto.Itens.Select(itemDto =>
            {
                var produto = produtos[itemDto.ProdutoId];

                return new ItemVenda
                {
                    ProdutoId = produto.Id,
                    Quantidade = itemDto.Quantidade,
                    ValorUnitario = produto.Valor,
                    Total = produto.Valor * itemDto.Quantidade
                };
            }).ToList()
        };

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await _context.Vendas.AddAsync(venda);

        await _context.SaveChangesAsync();

        try
        {
            var cobranca = await _pagamentoPixService.CriarCobranca(
                valorTotal,
                venda.Id.ToString());

            venda.PagamentoExternoId = cobranca.PagamentoId;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new PagamentoPixRespostaDTO
            {
                VendaId = venda.Id,
                ValorTotal = valorTotal,
                QrCode = cobranca.QrCode,
                QrCodeBase64 = cobranca.QrCodeBase64,
                ExpiraEm = cobranca.ExpiraEm
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex,
                "Falha ao criar cobrança PIX no Mercado Pago para a venda {VendaId}",
                venda.Id);

            return StatusCode(502,
                "Não foi possível gerar a cobrança PIX. Tente novamente.");
        }
    }

    [HttpPost("cartao/processar")]
    public async Task<ActionResult> ProcessarPagamentoCartao(IniciarPagamentoCartaoDTO dto)
    {
        if (!dto.Itens.Any())
        {
            return BadRequest("Venda sem itens");
        }

        if (dto.FormaPagamento is not (FormaPagamentoEnum.CartaoDebito or FormaPagamentoEnum.CartaoCredito))
        {
            return BadRequest("Forma de pagamento inválida para este endpoint");
        }

        if (string.IsNullOrWhiteSpace(dto.CardToken) || string.IsNullOrWhiteSpace(dto.PaymentMethodId))
        {
            return BadRequest("Dados do cartão não foram informados");
        }

        var caixa = await _context.Caixas
            .FirstOrDefaultAsync(x => x.Aberto);

        if (caixa == null)
        {
            return BadRequest(
                "Nenhum caixa aberto. Abra o caixa antes de registrar vendas.");
        }

        var usuarioId = int.Parse(
            User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var produtoIds = dto.Itens
            .Select(x => x.ProdutoId)
            .Distinct()
            .ToList();

        var produtos = await _context.Produtos
            .Where(x => produtoIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        decimal valorTotal = 0;

        foreach (var itemDto in dto.Itens)
        {
            if (!produtos.TryGetValue(itemDto.ProdutoId, out var produto))
            {
                return BadRequest(
                    $"Produto {itemDto.ProdutoId} não encontrado");
            }

            if (produto.Estoque < itemDto.Quantidade)
            {
                return BadRequest(
                    $"Estoque insuficiente para {produto.Descricao}");
            }

            valorTotal += produto.Valor * itemDto.Quantidade;
        }

        // Assim como no PIX, o estoque só é baixado se o pagamento for
        // aprovado (ver AprovarPagamento) — aqui só validamos disponibilidade.
        var venda = new Venda
        {
            DataVenda = DateTime.UtcNow,
            FormaPagamento = dto.FormaPagamento,
            StatusPagamento = StatusPagamentoEnum.Pendente,
            ValorTotal = valorTotal,
            CaixaId = caixa.Id,
            UsuarioId = usuarioId,
            CpfCnpjCliente = dto.CpfCliente,

            Itens = dto.Itens.Select(itemDto =>
            {
                var produto = produtos[itemDto.ProdutoId];

                return new ItemVenda
                {
                    ProdutoId = produto.Id,
                    Quantidade = itemDto.Quantidade,
                    ValorUnitario = produto.Valor,
                    Total = produto.Valor * itemDto.Quantidade
                };
            }).ToList()
        };

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        await _context.Vendas.AddAsync(venda);

        await _context.SaveChangesAsync();

        try
        {
            var tipoCartao = dto.FormaPagamento == FormaPagamentoEnum.CartaoCredito
                ? "credit_card"
                : "debit_card";

            var resultado = await _pagamentoCartaoService.ProcessarPagamento(
                valorTotal,
                venda.Id.ToString(),
                dto.CardToken,
                dto.PaymentMethodId,
                tipoCartao,
                dto.Parcelas,
                dto.CpfCliente);

            if (resultado.Status == "approved")
            {
                await transaction.CommitAsync();

                await AprovarPagamento(venda);

                return Ok(new PagamentoCartaoRespostaDTO
                {
                    VendaId = venda.Id,
                    ValorTotal = valorTotal,
                    StatusPagamento = venda.StatusPagamento
                });
            }

            venda.StatusPagamento = StatusPagamentoEnum.Recusado;

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return Ok(new PagamentoCartaoRespostaDTO
            {
                VendaId = venda.Id,
                ValorTotal = valorTotal,
                StatusPagamento = venda.StatusPagamento,
                MotivoRecusa = resultado.MotivoRecusa
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex,
                "Falha ao processar pagamento com cartão no Mercado Pago para a venda {VendaId}",
                venda.Id);

            return StatusCode(502,
                "Não foi possível processar o pagamento. Tente novamente.");
        }
    }

    [HttpGet("pix/{vendaId}/status")]
    public async Task<ActionResult> ConsultarStatusPix(int vendaId)
    {
        var venda = await _context.Vendas
            .FirstOrDefaultAsync(x => x.Id == vendaId);

        if (venda == null)
        {
            return NotFound();
        }

        if (venda.FormaPagamento != FormaPagamentoEnum.Pix ||
            venda.PagamentoExternoId == null)
        {
            return BadRequest("Venda não é um pagamento PIX");
        }

        // Já resolvido (aprovado/recusado/cancelado): não bate no Mercado
        // Pago de novo, evita reprocessar a baixa de estoque duas vezes.
        if (venda.StatusPagamento != StatusPagamentoEnum.Pendente)
        {
            return Ok(new StatusPagamentoPixRespostaDTO
            {
                Status = venda.StatusPagamento
            });
        }

        var statusExterno = await _pagamentoPixService
            .ConsultarStatus(venda.PagamentoExternoId);

        // Vocabulário da API de Orders: "processed" = pago, "action_required"
        // = aguardando o cliente pagar (continua pendente), "expired"/
        // "canceled" = não vai mais ser pago.
        switch (statusExterno)
        {
            case "processed":
                await AprovarPagamento(venda);
                break;

            case "expired":
            case "canceled":
            case "cancelled":
                venda.StatusPagamento = StatusPagamentoEnum.Cancelado;
                await _context.SaveChangesAsync();
                break;
        }

        return Ok(new StatusPagamentoPixRespostaDTO
        {
            Status = venda.StatusPagamento
        });
    }

    [HttpPost("pix/{vendaId}/cancelar")]
    public async Task<ActionResult> CancelarPagamentoPix(int vendaId)
    {
        var venda = await _context.Vendas
            .FirstOrDefaultAsync(x => x.Id == vendaId);

        if (venda == null)
        {
            return NotFound();
        }

        if (venda.StatusPagamento != StatusPagamentoEnum.Pendente)
        {
            return BadRequest("Esse pagamento não está mais pendente");
        }

        venda.StatusPagamento = StatusPagamentoEnum.Cancelado;

        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("{vendaId}/nfce/emitir")]
    public async Task<ActionResult> EmitirNfce(int vendaId, EmitirNfceDTO dto)
    {
        var venda = await _context.Vendas
            .Include(x => x.Itens)
            .ThenInclude(x => x.Produto)
            .FirstOrDefaultAsync(x => x.Id == vendaId);

        if (venda == null)
        {
            return NotFound();
        }

        if (venda.StatusPagamento != StatusPagamentoEnum.Aprovado)
        {
            return BadRequest("Só é possível emitir nota para vendas com pagamento aprovado");
        }

        if (venda.StatusEmissaoNfce is StatusEmissaoNfceEnum.Processando
            or StatusEmissaoNfceEnum.Autorizada)
        {
            return BadRequest("Essa venda já tem nota emitida ou em processamento");
        }

        venda.CpfCnpjCliente = dto.CpfCnpjCliente;
        venda.ReferenciaNfce = $"venda-{venda.Id}-{Guid.NewGuid():N}";
        venda.StatusEmissaoNfce = StatusEmissaoNfceEnum.Processando;
        venda.MotivoRejeicaoNfce = null;

        await _context.SaveChangesAsync();

        try
        {
            var resultado = await _nfceService.EmitirNfce(venda, venda.ReferenciaNfce);

            AplicarResultadoNfce(venda, resultado);

            await _context.SaveChangesAsync();

            return Ok(new StatusEmissaoNfceRespostaDTO
            {
                Status = venda.StatusEmissaoNfce,
                ChaveAcesso = venda.ChaveAcessoNfce,
                LinkDanfe = venda.LinkDanfeNfce,
                MotivoRejeicao = venda.MotivoRejeicaoNfce
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Falha ao emitir NFC-e na Focus NFe para a venda {VendaId}",
                venda.Id);

            venda.StatusEmissaoNfce = StatusEmissaoNfceEnum.Rejeitada;
            venda.MotivoRejeicaoNfce = "Falha de comunicação com o serviço de emissão";

            await _context.SaveChangesAsync();

            return StatusCode(502, "Não foi possível emitir a nota. Tente novamente.");
        }
    }

    [HttpGet("{vendaId}/nfce/status")]
    public async Task<ActionResult> ConsultarStatusNfce(int vendaId)
    {
        var venda = await _context.Vendas
            .FirstOrDefaultAsync(x => x.Id == vendaId);

        if (venda == null)
        {
            return NotFound();
        }

        if (venda.ReferenciaNfce == null)
        {
            return BadRequest("Essa venda ainda não teve emissão de nota iniciada");
        }

        // Já resolvido (autorizada/rejeitada/cancelada): não bate na Focus
        // NFe de novo.
        if (venda.StatusEmissaoNfce is not StatusEmissaoNfceEnum.Processando)
        {
            return Ok(new StatusEmissaoNfceRespostaDTO
            {
                Status = venda.StatusEmissaoNfce,
                ChaveAcesso = venda.ChaveAcessoNfce,
                LinkDanfe = venda.LinkDanfeNfce,
                MotivoRejeicao = venda.MotivoRejeicaoNfce
            });
        }

        var resultado = await _nfceService.ConsultarStatus(venda.ReferenciaNfce);

        AplicarResultadoNfce(venda, resultado);

        await _context.SaveChangesAsync();

        return Ok(new StatusEmissaoNfceRespostaDTO
        {
            Status = venda.StatusEmissaoNfce,
            ChaveAcesso = venda.ChaveAcessoNfce,
            LinkDanfe = venda.LinkDanfeNfce,
            MotivoRejeicao = venda.MotivoRejeicaoNfce
        });
    }

    // Vocabulário da Focus NFe: "processando_autorizacao" = segue
    // pendente, "autorizado" = nota válida, "erro_autorizacao"/"cancelado"
    // = não vai mais sair (cancelado só ocorre por ação nossa, a API não
    // cancela sozinha).
    private static void AplicarResultadoNfce(Venda venda, ResultadoEmissaoNfce resultado)
    {
        switch (resultado.Status)
        {
            case "autorizado":
                venda.StatusEmissaoNfce = StatusEmissaoNfceEnum.Autorizada;
                venda.ChaveAcessoNfce = resultado.ChaveAcesso;
                venda.LinkDanfeNfce = resultado.LinkDanfe;
                break;

            case "erro_autorizacao":
                venda.StatusEmissaoNfce = StatusEmissaoNfceEnum.Rejeitada;
                venda.MotivoRejeicaoNfce = resultado.MotivoRejeicao;
                break;

            case "cancelado":
                venda.StatusEmissaoNfce = StatusEmissaoNfceEnum.Cancelada;
                break;

            // "processando_autorizacao" e qualquer status desconhecido:
            // mantém Processando, tenta de novo na próxima consulta.
        }
    }

    [HttpGet]
    public async Task<ActionResult> ObterTodas()
    {
        var vendas = await _context.Vendas
            .OrderByDescending(x => x.DataVenda)
            .Select(x => new VendaListagemDTO
            {
                Id = x.Id,
                DataVenda = x.DataVenda,
                ValorTotal = x.ValorTotal,
                StatusPagamento = x.StatusPagamento
            })
            .ToListAsync();

        return Ok(vendas);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult> ObterPorId(int id)
    {
        var venda = await _context.Vendas
            .Include(x => x.Itens)
            .ThenInclude(x => x.Produto)
            .FirstOrDefaultAsync(x =>
                x.Id == id);

        if (venda == null)
        {
            return NotFound();
        }

        var response =
            new VendaDetalheDTO
            {
                Id = venda.Id,
                DataVenda = venda.DataVenda,
                ValorTotal = venda.ValorTotal,
                StatusPagamento = venda.StatusPagamento,

                Itens = venda.Itens
                    .Select(i =>
                        new ItemVendaRespostaDTO
                        {
                            ProdutoId = i.ProdutoId,
                            DescricaoProduto = i.Produto.Descricao,
                            Quantidade = i.Quantidade,
                            ValorUnitario = i.ValorUnitario,
                            Total = i.Total
                        })
                    .ToList()
            };

        return Ok(response);
    }

    private async Task AprovarPagamento(Venda venda)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        foreach (var item in venda.Itens)
        {
            // O pagamento já caiu — se por algum motivo o estoque não bater
            // mais, seguimos aprovando (o dinheiro já foi recebido) e só
            // registramos o quanto foi possível baixar.
            await _context.Produtos
                .Where(x => x.Id == item.ProdutoId && x.Estoque >= item.Quantidade)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    p => p.Estoque,
                    p => p.Estoque - item.Quantidade));
        }

        venda.StatusPagamento = StatusPagamentoEnum.Aprovado;

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        await _hub.Clients.All.SendAsync(
            "VendaCriada",
            new
            {
                venda.Id,
                venda.ValorTotal,
                venda.DataVenda
            });
    }

    private async Task<string?> MontarItensComBaixaDeEstoque(
        Venda venda,
        List<ItemVendaDTO> itensDto,
        Dictionary<int, Produto> produtos)
    {
        decimal valorTotal = 0;

        foreach (var itemDto in itensDto)
        {
            var produto = produtos[itemDto.ProdutoId];

            // Baixa condicional no banco (WHERE Estoque >= quantidade) para
            // evitar estoque negativo quando vários caixas vendem o mesmo
            // produto ao mesmo tempo.
            var linhasAfetadas = await _context.Produtos
                .Where(x =>
                    x.Id == produto.Id &&
                    x.Estoque >= itemDto.Quantidade)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    p => p.Estoque,
                    p => p.Estoque - itemDto.Quantidade));

            if (linhasAfetadas == 0)
            {
                return $"Estoque insuficiente para {produto.Descricao}";
            }

            var totalItem = produto.Valor * itemDto.Quantidade;

            valorTotal += totalItem;

            venda.Itens.Add(new ItemVenda
            {
                ProdutoId = produto.Id,
                Quantidade = itemDto.Quantidade,
                ValorUnitario = produto.Valor,
                Total = totalItem
            });
        }

        venda.ValorTotal = valorTotal;

        return null;
    }
}
