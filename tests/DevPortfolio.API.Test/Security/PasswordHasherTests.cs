using DevPortfolio.Infrastructure.Security;

namespace DevPortfolio.API.Test.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void Nao_deve_gravar_a_senha_em_texto_puro()
    {
        var hash = _hasher.GerarHash("Senha@123");

        Assert.DoesNotContain("Senha@123", hash);
    }

    [Fact]
    public void Deve_gerar_hashes_diferentes_para_a_mesma_senha()
    {
        Assert.NotEqual(_hasher.GerarHash("Senha@123"), _hasher.GerarHash("Senha@123"));
    }

    [Fact]
    public void Deve_verificar_a_senha_correta_e_recusar_a_incorreta()
    {
        var hash = _hasher.GerarHash("Senha@123");

        Assert.True(_hasher.Verificar(hash, "Senha@123"));
        Assert.False(_hasher.Verificar(hash, "senha@123"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("hash-invalido")]
    public void Deve_recusar_hash_vazio_ou_invalido(string hash)
    {
        Assert.False(_hasher.Verificar(hash, "Senha@123"));
    }
}
