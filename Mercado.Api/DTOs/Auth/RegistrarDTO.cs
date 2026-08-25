namespace Mercado.Api.DTOs.Auth;

public class RegistrarDTO
{
    public string Role { get; set; } = "Caixa";

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}