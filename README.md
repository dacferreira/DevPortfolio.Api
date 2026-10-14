# DevPortfolio.Api

API do DevPortfolio em ASP.NET Core 8, organizada em camadas (Clean Architecture).

```
src/
├── DevPortfolio.Domain/          entidades (Usuario, TokenRedefinicaoSenha), enums e interfaces de repositório
├── DevPortfolio.Application/     DTOs, AuthService, ContaService e abstrações (IPasswordHasher, IEmailService)
├── DevPortfolio.Infrastructure/  EF Core + MySQL (Pomelo), repositórios e hash de senha
├── DevPortfolio.Comunicacoes/    comunicações externas: e-mail via Resend (templates em Templates/*.html)
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
| POST | `/api/conta/registro` | público | Cria conta com perfil Visitante e envia e-mail de boas-vindas |
| POST | `/api/conta/esqueci-senha` | público | Envia o link de redefinição (resposta igual para qualquer e-mail) |
| POST | `/api/conta/redefinir-senha` | público | Troca a senha com o token do link |

A documentação completa fica no Swagger, em desenvolvimento: `http://localhost:5128/swagger`.

Todas as respostas seguem o envelope `{ sucesso, dados, erro: { codigo, mensagem, detalhes } }`.
Códigos de erro: `CREDENCIAIS_INVALIDAS` (401), `USUARIO_INATIVO` (403), `NAO_AUTENTICADO` (401),
`DADOS_INVALIDOS` (400), `CSRF_INVALIDO` (400), `TOKEN_INVALIDO` (400), `EMAIL_JA_CADASTRADO` (409),
`MUITAS_TENTATIVAS` (429) e `ERRO_INTERNO` (500).

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
- **Cadastro:** contas novas sempre têm perfil Visitante; só o administrador inicial é Administrador.
  Senhas novas exigem de 8 a 128 caracteres, com letras e números.
- **Redefinição de senha:** token aleatório de 32 bytes, válido por 30 minutos e de uso único. O banco guarda só o SHA-256
  do token, e um novo pedido invalida os links anteriores.
- **E-mails:** enviados em segundo plano (fila em memória). A resposta da API não depende do envio, então o tempo de
  resposta não revela se o e-mail existe, e uma falha do Resend não derruba a requisição (fica registrada no log).
  Os valores inseridos nos templates são codificados em HTML.
- **Limites:** cadastro e recuperação de senha têm limite de tentativas por IP próprio, separado do login.

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
| `Seguranca:QuantidadeProxies` | `Seguranca__QuantidadeProxies` | Proxies reversos na frente da API (padrão 0 = desligado). Com Vercel + host da API, use 2 |
| `Conta:ExpiracaoTokenRedefinicaoMinutos` | `Conta__ExpiracaoTokenRedefinicaoMinutos` | Validade do link de redefinição (padrão 30) |
| `Email:ResendApiKey` | `Email__ResendApiKey` | Chave da API do Resend (**segredo**). Sem ela, os e-mails não são enviados (aviso no log) |
| `Email:Remetente` | `Email__Remetente` | Remetente dos e-mails. `onboarding@resend.dev` só entrega para o dono da conta do Resend; para outros destinatários, verifique um domínio no Resend |
| `Email:UrlAplicacao` | `Email__UrlAplicacao` | Endereço do front, usado nos links dos e-mails (muda por ambiente) |

Segredos nunca vão para o `appsettings.json`. Em desenvolvimento, use user-secrets:

```bash
cd src/DevPortfolio.API
dotnet user-secrets set "ConnectionStrings:DevPortfolio" "Server=localhost;Port=3306;Database=devportfolio;User=<usuario>;Password=<senha>"
dotnet user-secrets set "UsuarioInicial:Nome" "Diogo"
dotnet user-secrets set "UsuarioInicial:Email" "<seu-email>"
dotnet user-secrets set "UsuarioInicial:Senha" "<sua-senha>"
dotnet user-secrets set "Email:ResendApiKey" "<chave-do-resend>"
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

## Produção (front no Vercel)

O front fica no Vercel e encaminha `/api/*` para a API (rewrite no `vercel.json` do front), mantendo tudo na mesma
origem para os cookies SameSite=Strict e o token CSRF. A API roda em outro host (o Vercel não executa .NET) com:

- `ConnectionStrings__DevPortfolio`: MySQL acessível pelo host da API.
- `Email__ResendApiKey` e `Email__UrlAplicacao`, esta com a URL pública do front no Vercel.
- `Seguranca__QuantidadeProxies=2` (Vercel + balanceador do host), para a API reconhecer HTTPS e o IP real do cliente.
- Banco criado com o Flyway (`DevPortfolio.Dados`).

Persista as chaves de Data Protection (ex.: volume ou banco) para que as sessões sobrevivam a reinícios.

## Testes

```bash
dotnet test DevPortfolio.sln
dotnet test DevPortfolio.sln --collect:"XPlat Code Coverage" --results-directory ./cobertura
```

O pipeline (`.github/workflows/ci.yml`) roda build, testes e publica a cobertura.
