# DevPortfolio.Api

API do DevPortfolio em ASP.NET Core 8, organizada em camadas (Clean Architecture).

```
src/
├── DevPortfolio.Domain/          entidades (Usuario), enums e interfaces de repositório
├── DevPortfolio.Application/     DTOs, AuthService e abstrações (IPasswordHasher)
├── DevPortfolio.Infrastructure/  EF Core + MySQL (Pomelo), repositórios e hash de senha
└── DevPortfolio.API/             controllers, sessão, CSRF, rate limiting e Swagger
tests/
├── DevPortfolio.Domain.Test/
├── DevPortfolio.Application.Test/
└── DevPortfolio.API.Test/        testes de integração (WebApplicationFactory + SQLite em memória)
```

## Endpoints

| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| GET | `/api/auth/csrf` | público | Emite o token anti-CSRF no cookie `XSRF-TOKEN` |
| POST | `/api/auth/login` | público | Autentica e inicia a sessão |
| POST | `/api/auth/logout` | autenticado | Encerra a sessão |
| GET | `/api/auth/me` | autenticado | Usuário da sessão atual |

A documentação completa fica no Swagger, em desenvolvimento: `http://localhost:5128/swagger`.

Todas as respostas seguem o envelope `{ sucesso, dados, erro: { codigo, mensagem, detalhes } }`.
Códigos de erro: `CREDENCIAIS_INVALIDAS` (401), `USUARIO_INATIVO` (403), `NAO_AUTENTICADO` (401),
`DADOS_INVALIDOS` (400), `CSRF_INVALIDO` (400), `MUITAS_TENTATIVAS` (429) e `ERRO_INTERNO` (500).

## Segurança

- **Senhas:** hash PBKDF2 (HMAC-SHA512, salt por senha, 100.000 iterações) do ASP.NET Core Identity. Nunca em texto puro.
- **Sessão:** cookie `devportfolio.sessao` HttpOnly, Secure e SameSite=Strict, com expiração deslizante.
  "Lembrar de mim" torna o cookie persistente. Nenhum token fica no `localStorage`.
- **CSRF:** todo POST/PUT/PATCH/DELETE exige o header `X-XSRF-TOKEN` com o valor do cookie `XSRF-TOKEN`
  (o `HttpClient` do Angular faz isso automaticamente). O token é renovado no login e no logout.
- **Força bruta:** limite de tentativas de login por IP (padrão: 5 por minuto).
- **Enumeração de usuários:** usuário inexistente e senha incorreta têm a mesma resposta e o mesmo custo de verificação.
  O status "inativo" só é informado quando a senha confere.
- **Autorização:** cada endpoint protegido usa `[Authorize]`; `/api/auth/me` também confere se o usuário continua ativo.

## Configuração

| Chave | Variável de ambiente | Descrição |
|---|---|---|
| `ConnectionStrings:DevPortfolio` | `ConnectionStrings__DevPortfolio` | Connection string do MySQL (**segredo**) |
| `UsuarioInicial:Nome` | `UsuarioInicial__Nome` | Nome do administrador criado na inicialização (opcional) |
| `UsuarioInicial:Email` | `UsuarioInicial__Email` | E-mail do administrador inicial |
| `UsuarioInicial:Senha` | `UsuarioInicial__Senha` | Senha do administrador inicial (**segredo**) |
| `Seguranca:ExigirHttps` | `Seguranca__ExigirHttps` | Cookies sempre Secure (padrão `true`; `false` só no `appsettings.Development.json`, em que a API roda em HTTP) |
| `Seguranca:DuracaoSessaoMinutos` | `Seguranca__DuracaoSessaoMinutos` | Duração da sessão (padrão 60) |
| `Seguranca:LimiteTentativasLogin` | `Seguranca__LimiteTentativasLogin` | Tentativas de login por IP na janela (padrão 5) |
| `Seguranca:JanelaTentativasLoginSegundos` | `Seguranca__JanelaTentativasLoginSegundos` | Janela do limite (padrão 60) |

Segredos nunca vão para o `appsettings.json`. Em desenvolvimento, use user-secrets:

```bash
cd src/DevPortfolio.API
dotnet user-secrets set "ConnectionStrings:DevPortfolio" "Server=localhost;Port=3306;Database=devportfolio;User=<usuario>;Password=<senha>"
dotnet user-secrets set "UsuarioInicial:Nome" "Diogo"
dotnet user-secrets set "UsuarioInicial:Email" "<seu-email>"
dotnet user-secrets set "UsuarioInicial:Senha" "<sua-senha>"
```

O administrador inicial só é criado se o e-mail ainda não existir. Depois do primeiro acesso, as chaves
`UsuarioInicial:*` podem ser removidas.

## Executando

1. Crie o banco com o Flyway (repositório `DevPortfolio.Dados`): `flyway -configFiles=flyway.conf migrate`.
2. Configure os user-secrets acima.
3. Suba a API:

```bash
dotnet run --project src/DevPortfolio.API --launch-profile http
```

4. Suba o front (`DevPortfolio.Ui/devportfolio-ui`) com `npm start`: o `ng serve` encaminha `/api` para
   `http://localhost:5128`, mantendo front e API na mesma origem (necessário para os cookies SameSite=Strict).

Em produção, sirva o front e a API no mesmo domínio (ex.: proxy reverso com `/api` apontando para a API).
Atrás de proxy reverso, configure `ForwardedHeaders` para que o limite de login use o IP real do cliente,
e persista as chaves de Data Protection para que as sessões sobrevivam a reinícios.

## Testes

```bash
dotnet test DevPortfolio.sln
dotnet test DevPortfolio.sln --collect:"XPlat Code Coverage" --results-directory ./cobertura
```

O pipeline (`.github/workflows/ci.yml`) roda build, testes e publica a cobertura.
