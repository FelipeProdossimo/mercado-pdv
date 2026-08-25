using Mercado.Api.DTOs;
using Mercado.Api.DTOs.Produto;

namespace Mercado.Api.Services;

public interface IProdutoService
{
    Task <PaginacaoRespostaDTO<ProdutoDTO>> ObterTodos(PaginacaoDTO dto);

    Task<int> Adicionar(ProdutoCriarDTO dto);
}