using Mercado.Api.Context;
using Mercado.Api.DTOs;
using Mercado.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Mercado.Api.Repositories;

public class ProdutoRepository : IProdutoRepository
{
    private readonly AppDbContext _context;

    public ProdutoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task <(List<Produto>, int totalRegistros)> ObterTodos(PaginacaoDTO dto)
    {
        var query = _context.Produtos.Where(x => x.Ativo).AsQueryable();

        if (!string.IsNullOrWhiteSpace(dto.Descricao))
        {
            query = query.Where(x =>
                x.Descricao.ToLower()
                    .Contains(dto.Descricao.ToLower()));
        }

        var totalRegistros = await query.CountAsync();

        query = query
            .Skip((dto.Pagina - 1) * dto.TamanhoPagina)
            .Take(dto.TamanhoPagina);

        var produtos = await query.ToListAsync();

        return (produtos, totalRegistros);
    }

    public async Task Adicionar(Produto produto)
    {
        await _context.Produtos.AddAsync(produto);

        await _context.SaveChangesAsync();
    }
}