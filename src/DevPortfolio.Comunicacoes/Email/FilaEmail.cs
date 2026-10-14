using System.Threading.Channels;

namespace DevPortfolio.Comunicacoes.Email;

// Fila em memória: a requisição só enfileira e responde na hora; o envio acontece no EnvioEmailWorker.
// Assim o tempo de resposta não revela se um e-mail foi enviado, e falhas do provedor não derrubam a requisição.
public class FilaEmail
{
    private readonly Channel<MensagemEmail> _canal = Channel.CreateUnbounded<MensagemEmail>(
        new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnfileirarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default) =>
        _canal.Writer.WriteAsync(mensagem, cancellationToken);

    public IAsyncEnumerable<MensagemEmail> LerTodasAsync(CancellationToken cancellationToken) =>
        _canal.Reader.ReadAllAsync(cancellationToken);
}
