namespace DevPortfolio.Application.Conta.Dtos;

// Regra de senha das contas novas e das redefinições (mesma regra validada no front).
public static class RegrasSenha
{
    public const string Padrao = @"^(?=.*[A-Za-z])(?=.*\d).{8,128}$";
    public const string Mensagem = "A senha deve ter de 8 a 128 caracteres, com letras e números.";
}
