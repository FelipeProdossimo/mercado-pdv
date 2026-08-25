namespace Mercado.Api.Entities;

public class Usuario
{
    public int Id { get; set; }

    public string Role { get; set; } = "Caixa";

    public string Nome { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}