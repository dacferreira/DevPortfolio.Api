namespace DevPortfolio.Comunicacoes.Email;

/// <param name="Tipo">Identifica o e-mail nos logs (o destinatário não é registrado).</param>
public record MensagemEmail(string Tipo, string Para, string Assunto, string Html);
