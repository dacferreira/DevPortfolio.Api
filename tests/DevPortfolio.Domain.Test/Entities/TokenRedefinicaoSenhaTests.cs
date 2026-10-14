using DevPortfolio.Domain.Entities;

namespace DevPortfolio.Domain.Test.Entities;

public class TokenRedefinicaoSenhaTests
{
    private static readonly DateTime Agora = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Deve_ser_valido_antes_da_expiracao()
    {
        var token = new TokenRedefinicaoSenha(1, "hash", Agora.AddMinutes(30), Agora);

        Assert.True(token.EstaValido(Agora.AddMinutes(29)));
    }

    [Fact]
    public void Deve_expirar()
    {
        var token = new TokenRedefinicaoSenha(1, "hash", Agora.AddMinutes(30), Agora);

        Assert.False(token.EstaValido(Agora.AddMinutes(30)));
    }

    [Fact]
    public void Deve_ser_de_uso_unico()
    {
        var token = new TokenRedefinicaoSenha(1, "hash", Agora.AddMinutes(30), Agora);

        token.MarcarComoUtilizado(Agora.AddMinutes(1));

        Assert.False(token.EstaValido(Agora.AddMinutes(2)));
        Assert.Equal(Agora.AddMinutes(1), token.DataUtilizacao);
    }

    [Fact]
    public void Deve_exigir_hash_e_expiracao_futura()
    {
        Assert.Throws<ArgumentException>(() => new TokenRedefinicaoSenha(1, "", Agora.AddMinutes(30), Agora));
        Assert.Throws<ArgumentException>(() => new TokenRedefinicaoSenha(1, "hash", Agora, Agora));
    }
}
