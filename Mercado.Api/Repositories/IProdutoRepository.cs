using Mercado.Api.DTOs;
using Mercado.Api.Entities;

namespace Mercado.Api.Repositories;

public interface IProdutoRepository
{
    Task <(List<Produto>, int totalRegistros)> ObterTodos(PaginacaoDTO dto);

    Task Adicionar(Produto produto);
}