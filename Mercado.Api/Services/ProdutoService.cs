using Mercado.Api.DTOs;
using Mercado.Api.DTOs.Produto;
using Mercado.Api.Entities;
using Mercado.Api.Repositories;

namespace Mercado.Api.Services;

public class ProdutoService : IProdutoService
{
    private readonly IProdutoRepository _repository;

    public ProdutoService(IProdutoRepository repository)
    {
        _repository = repository;
    }

    public async Task <PaginacaoRespostaDTO<ProdutoDTO>> ObterTodos(PaginacaoDTO dto)
    {
        var (produtos, totalRegistros) =
            await _repository.ObterTodos(dto);

        var dados = produtos.Select(p => new ProdutoDTO
        {
            Id = p.Id,
            Descricao = p.Descricao,
            Valor = p.Valor,
            Estoque = p.Estoque,
            CodigoBarras = p.CodigoBarras,
            ImagemUrl = p.ImagemUrl
        }).ToList();

        return new PaginacaoRespostaDTO<ProdutoDTO>
        {
            Dados = dados,
            TotalRegistros = totalRegistros,
            Pagina = dto.Pagina,
            TamanhoPagina = dto.TamanhoPagina
        };
    }

    public async Task<int> Adicionar(ProdutoCriarDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Descricao))
        {
            throw new Exception("Descrição é obrigatória");
        }

        if (dto.Valor <= 0)
        {
            throw new Exception("Valor deve ser maior que zero");
        }

        var produto = new Produto
        {
            Descricao = dto.Descricao,
            Valor = dto.Valor,
            Estoque = dto.Estoque,
            CodigoBarras = dto.CodigoBarras
        };

        await _repository.Adicionar(produto);

        return produto.Id;
    }
}