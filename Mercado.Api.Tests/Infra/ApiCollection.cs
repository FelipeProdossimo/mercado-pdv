namespace Mercado.Api.Tests.Infra;

// Todos os testes compartilham a mesma API e o mesmo banco, então rodam em
// sequência (sem paralelismo) e cada um limpa o banco antes de começar.
[CollectionDefinition(Nome)]
public class ApiCollection : ICollectionFixture<MercadoApiFactory>
{
    public const string Nome = "API";
}

[Collection(ApiCollection.Nome)]
public abstract class TesteDeIntegracao : IAsyncLifetime
{
    protected MercadoApiFactory Api { get; }

    protected HttpClient Client { get; private set; } = null!;

    protected TesteDeIntegracao(MercadoApiFactory api)
    {
        Api = api;
    }

    public async Task InitializeAsync()
    {
        await Api.LimparBanco();
        Client = await Api.CriarClienteAutenticado();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
