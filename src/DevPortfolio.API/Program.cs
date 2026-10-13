using DevPortfolio.API.Configuration;
using DevPortfolio.Application;
using DevPortfolio.Infrastructure;
using DevPortfolio.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// A connection string (com senha) vem de user-secrets ou da variável ConnectionStrings__DevPortfolio.
var connectionString = builder.Configuration.GetConnectionString("DevPortfolio");
if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
{
    throw new InvalidOperationException(
        $"Connection string 'DevPortfolio' não configurada (ambiente: {builder.Environment.EnvironmentName}). " +
        "Use 'dotnet user-secrets set \"ConnectionStrings:DevPortfolio\" ...' " +
        "ou a variável de ambiente ConnectionStrings__DevPortfolio (veja o README).");
}

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString ?? string.Empty);
builder.Services.AddSeguranca(builder.Configuration);
builder.Services.AddControllers().AddRespostasPadronizadas();
builder.Services.AddSwaggerDocumentado();

var app = builder.Build();

app.UseTratamentoDeErros();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // Em desenvolvimento o front acessa a API por HTTP via proxy do Angular (localhost:5128).
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var usuarioInicial = app.Configuration.GetSection(UsuarioInicialOptions.Secao).Get<UsuarioInicialOptions>();
if (usuarioInicial?.Configurado == true)
{
    await UsuarioInicialSeeder.ExecutarAsync(app.Services, usuarioInicial.Nome!, usuarioInicial.Email!, usuarioInicial.Senha!);
}

app.Run();

// Exposto para os testes de integração (WebApplicationFactory).
public partial class Program;
