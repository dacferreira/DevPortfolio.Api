namespace DevPortfolio.Application.Test.Fakes;

public class RelogioFake : TimeProvider
{
    public DateTimeOffset Agora { get; set; } = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    public override DateTimeOffset GetUtcNow() => Agora;

    public void Avancar(TimeSpan tempo) => Agora += tempo;
}
