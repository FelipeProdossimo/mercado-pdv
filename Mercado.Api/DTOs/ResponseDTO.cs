namespace Mercado.Api.DTOs;

public class ResponseDTO<T>
{
    public bool Sucesso { get; set; }

    public string Mensagem { get; set; } = string.Empty;

    public T? Dados { get; set; }
}