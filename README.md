# VaccinationControl

Sistema para gerenciamento de vacinação, permitindo o controle de pessoas, vacinas, aplicações e histórico de vacinação.

> **Estado atual:** backend em desenvolvimento. Os cadastros de vacinas e de pessoas estão
> funcionando de ponta a ponta, incluindo a remoção de pessoa com exclusão em cascata do
> cartão. O registro de vacinação e a autenticação ainda não foram implementados — veja
> [Status e próximos passos](#status-e-próximos-passos).

## Tecnologias

### Backend — em uso

* .NET 10
* ASP.NET Core
* Entity Framework Core
* SQLite
* MediatR (CQRS)
* FluentValidation
* xUnit
* Scalar (documentação da API)

### Frontend — planejado

Ainda não iniciado. A pasta `frontend/` existe, mas está vazia.

* React
* TypeScript
* Vite
* Axios

### DevOps

* GitHub Actions
* Git

---

## Status e próximos passos

| Etapa | Situação |
| --- | --- |
| Estrutura em Clean Architecture (4 projetos) | Concluída |
| Entidades de domínio e exceções | Concluída |
| EF Core + SQLite, mappers e primeira migration | Concluída |
| Pipeline de validação (MediatR + FluentValidation) | Concluída |
| Tratamento global de exceções | Concluída |
| Cadastro e consulta de vacinas | Concluída |
| Cadastro e remoção de pessoas | Concluída |
| Registro de vacinação com validação de dose | Planejada |
| Consulta e exclusão do cartão de vacinação | Planejada |
| Autenticação JWT | Planejada |
| Testes unitários e de integração | Planejada |
| Frontend em React | Planejada |

---

## Arquitetura

O backend utiliza uma arquitetura baseada em separação de responsabilidades:

```text
backend/
├── src/
│   ├── VaccinationControl.Api/
│   ├── VaccinationControl.Application/
│   ├── VaccinationControl.Domain/
│   └── VaccinationControl.Infrastructure/
│
└── tests/
    ├── VaccinationControl.UnitTests/
    └── VaccinationControl.IntegrationTests/
```

### Camadas

**Domain**

Contém as regras e entidades principais do negócio.

**Application**

Contém casos de uso, CQRS, handlers, DTOs, validações e interfaces.

**Infrastructure**

Contém implementações relacionadas a banco de dados, EF Core, repositories e serviços externos.

**API**

Responsável pelos endpoints HTTP, configuração da aplicação, autenticação e middleware.

### Direção das dependências

```text
Api ──> Application ──> Domain
 └────> Infrastructure ──> Application, Domain
```

As setas apontam sempre para dentro. O Domain não referencia nada — nem pacote NuGet. A
Application não conhece Entity Framework nem qualquer tipo da Infrastructure: ela **declara**
as interfaces de persistência (`IVaccineRepository`, `IPersonRepository`, `IUnitOfWork`) e a
Infrastructure as implementa.

Na prática isso significa que trocar SQLite por outro banco, ou o MediatR por outro mediador,
não toca em nenhuma regra de negócio.

### Composição da injeção de dependência

Cada camada expõe um método de extensão que registra os próprios serviços, em um arquivo
`DependencyInjection.cs`:

| Camada | Método | Registra |
| --- | --- | --- |
| Application | `AddApplication()` | MediatR, `ValidationBehavior`, validators do FluentValidation |
| Infrastructure | `AddInfrastructure(configuration, contentRootPath)` | `AppDbContext`, `IUnitOfWork`, repositórios |

Com isso o `Program.cs` compõe a aplicação em duas linhas e não precisa conhecer `AppDbContext`,
`VaccineRepository` nem que o MediatR existe:

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
```

As duas classes se chamam `DependencyInjection` — não há conflito porque estão em namespaces
diferentes, e o nome nunca aparece no código de chamada: são métodos de extensão resolvidos
sobre `IServiceCollection`.

### Fluxo de uma requisição

```text
Controller
    ↓ ISender.Send(command)
MediatR
    ↓
ValidationBehavior           valida o command com FluentValidation
    ↓
Handler                      regras que dependem do estado (conflitos, existência)
    ↓
Repositório  →  AppDbContext  →  SQLite
```

O controller é deliberadamente fino: recebe o command, delega ao MediatR e traduz o retorno em
`IActionResult`. Nenhuma regra de negócio vive nele.

### Validação

O `ValidationBehavior` roda antes de **todo** handler e valida o command com o validator
correspondente. Se houver falha, lança `ValidationException` — traduzida em `400` com a lista
de campos.

Regras que dependem do banco (documento já cadastrado, dose duplicada) não cabem em um
validator, porque exigem consulta e resultam em `409`, não `400`. Elas ficam no handler e
lançam exceções de domínio.

Antes de persistir, o handler ainda valida a **entidade** com seu próprio validator
(`IValidator<Vaccine>`), como rede de segurança: nenhuma entidade chega ao banco sem passar
pelas suas regras, independentemente de qual caso de uso a criou.

### Tratamento de erros

O `GlobalExceptionHandler` é o único lugar que converte exceção em status HTTP, devolvendo
sempre `ProblemDetails`:

| Exceção | HTTP |
| --- | --- |
| `ValidationException` (FluentValidation) | 400 |
| `NotFoundException` | 404 |
| `ConflictException` | 409 |
| `DomainException` | 422 |
| qualquer outra | 500 |

As exceções de domínio ficam no Domain e não conhecem HTTP — o mapeamento é responsabilidade
da camada de API.

---

## API

Documentação interativa disponível em desenvolvimento:

| Recurso | URL |
| --- | --- |
| Scalar (UI) | `http://localhost:5201/scalar/v1` |
| Documento OpenAPI | `http://localhost:5201/openapi/v1.json` |

Os recursos de vacinas e pessoas estão implementados. O registro de vacinação, a consulta do
cartão e a autenticação constam do plano e ainda não existem.

### Vacinas

#### `POST /api/vaccines`

Cadastra uma vacina.

```json
{ "name": "Hepatite B" }
```

| HTTP | Quando |
| --- | --- |
| 201 | Vacina cadastrada; `Location` aponta para o recurso |
| 400 | Nome vazio ou com mais de 200 caracteres |
| 409 | Já existe vacina com esse nome |

```json
{ "id": "3df1340d-3381-4021-a782-18679e777c50", "name": "Hepatite B" }
```

#### `GET /api/vaccines/{id}`

Consulta uma vacina pelo identificador. É o endereço devolvido no `Location` do cadastro.

| HTTP | Quando |
| --- | --- |
| 200 | Vacina encontrada |
| 400 | Identificador vazio (`00000000-...`) |
| 404 | Não existe vacina com esse identificador |

```json
{ "id": "3df1340d-3381-4021-a782-18679e777c50", "name": "Hepatite B" }
```

#### `GET /api/vaccines`

Lista as vacinas cadastradas, ordenadas por nome. Todos os parâmetros são opcionais — sem
nenhum deles, devolve o catálogo inteiro.

| Parâmetro | Tipo | Descrição |
| --- | --- | --- |
| `search` | string | Filtra por trecho do nome. Não diferencia maiúsculas de minúsculas |
| `page` | int | Página desejada, a partir de 1. Padrão 1 |
| `pageSize` | int | Itens por página, de 1 a 100. Padrão 20 |

Informar `page` **ou** `pageSize` ativa a paginação; o que faltar assume o padrão.

A resposta usa sempre o mesmo envelope, com ou sem paginação. Quando não há paginação,
`pageSize` reflete o total devolvido:

```json
{
  "items": [
    { "id": "6e63817f-d4ea-42cd-82d2-7d6309fde8cd", "name": "Hepatite A" },
    { "id": "99cdafd0-2213-4f59-b322-2cf8c8aa3aa8", "name": "Hepatite B" }
  ],
  "page": 1,
  "pageSize": 3,
  "totalCount": 6,
  "totalPages": 2
}
```

`totalCount` é o total que atende ao filtro, não o tamanho da página.

| HTTP | Quando |
| --- | --- |
| 200 | Consulta realizada |
| 400 | `page` menor que 1, `pageSize` fora de 1–100 ou `search` acima de 200 caracteres |

### Pessoas

#### `POST /api/people`

Cadastra uma pessoa. O documento é o número de identificação único — CPF, RG ou matrícula;
o formato não é imposto.

```json
{ "name": "Maria Silva", "document": "12345678901" }
```

| HTTP | Quando |
| --- | --- |
| 201 | Pessoa cadastrada; `Location` aponta para o recurso |
| 400 | Nome ou documento vazios, ou acima do tamanho permitido |
| 409 | Já existe pessoa com esse documento |

```json
{
  "id": "94549402-7498-483b-b31a-da2c40d471ce",
  "name": "Maria Silva",
  "document": "12345678901"
}
```

#### `GET /api/people/{id}`

Consulta uma pessoa pelo identificador. É o endereço devolvido no `Location` do cadastro.

| HTTP | Quando |
| --- | --- |
| 200 | Pessoa encontrada |
| 400 | Identificador vazio (`00000000-...`) |
| 404 | Não existe pessoa com esse identificador |

#### `DELETE /api/people/{id}`

Remove a pessoa. **A exclusão é em cascata**: o cartão de vacinação e todos os registros
associados são apagados junto, pelo `ON DELETE CASCADE` da chave estrangeira. As vacinas do
catálogo não são afetadas.

| HTTP | Quando |
| --- | --- |
| 204 | Pessoa removida; sem corpo na resposta |
| 400 | Identificador vazio (`00000000-...`) |
| 404 | Não existe pessoa com esse identificador |

### Exemplos de chamada

```bash
# cadastrar uma vacina
curl -X POST http://localhost:5201/api/vaccines \
  -H "Content-Type: application/json" \
  -d '{"name":"Hepatite B"}'

# consultar pelo id
curl http://localhost:5201/api/vaccines/3df1340d-3381-4021-a782-18679e777c50

# catálogo inteiro
curl http://localhost:5201/api/vaccines

# buscando por trecho do nome
curl "http://localhost:5201/api/vaccines?search=hepat"

# segunda página, três por página
curl "http://localhost:5201/api/vaccines?page=2&pageSize=3"

# busca e paginação combinadas
curl "http://localhost:5201/api/vaccines?search=hepat&pageSize=1"
```

```bash
# cadastrar uma pessoa
curl -X POST http://localhost:5201/api/people \
  -H "Content-Type: application/json" \
  -d '{"name":"Maria Silva","document":"12345678901"}'

# consultar pelo id
curl http://localhost:5201/api/people/94549402-7498-483b-b31a-da2c40d471ce

# remover a pessoa e todo o seu cartão de vacinação
curl -X DELETE http://localhost:5201/api/people/94549402-7498-483b-b31a-da2c40d471ce
```

---

## Estrutura do projeto

```text
VaccinationControl/
│
├── .github/
│   └── workflows/
│       └── ci.yml
│
├── backend/
│   ├── src/
│   │   ├── VaccinationControl.Api/            controllers, middleware, composição
│   │   ├── VaccinationControl.Application/    commands, queries, validators, interfaces
│   │   ├── VaccinationControl.Domain/         entidades e exceções de negócio
│   │   └── VaccinationControl.Infrastructure/ DbContext, mappers, repositórios
│   │
│   ├── tests/
│   │   ├── VaccinationControl.UnitTests/
│   │   └── VaccinationControl.IntegrationTests/
│   │
│   └── VaccinationControl.slnx
│
├── frontend/                                  vazia; ainda não iniciado
│
├── .editorconfig
├── .gitignore
└── README.md
```

Dentro da Application, cada caso de uso tem a própria pasta com command/query, handler e
validator juntos:

```text
Application/People/
├── PersonResponse.cs
├── Commands/
│   ├── CreatePerson/
│   └── DeletePerson/
└── Queries/
    └── GetPersonById/
```

---

## Pré-requisitos

Para o backend, que é o que existe hoje:

* .NET 10 SDK
* Git
* `dotnet-ef` — necessário apenas para criar ou aplicar migrations:

```bash
dotnet tool install --global dotnet-ef
```

Node.js e npm entram quando o frontend for iniciado.

---

## Executando o Backend

> **Todos os comandos desta seção rodam a partir de `backend/`.** Os caminhos como
> `src/VaccinationControl.Api` são relativos a essa pasta — executados da raiz do
> repositório, o .NET responde *"O caminho do arquivo fornecido não existe"*.

Entre na pasta:

```bash
cd backend
```

Restaure as dependências:

```bash
dotnet restore
```

Execute os testes:

```bash
dotnet test
```

Execute a API:

```bash
dotnet run --project src/VaccinationControl.Api
```

A API sobe em `http://localhost:5201` e `https://localhost:7277`.

Para rodar sem entrar na pasta, use o caminho completo a partir da raiz:

```bash
dotnet run --project backend/src/VaccinationControl.Api
```

---

## Executando o Frontend

> Ainda não implementado. A pasta `frontend/` está vazia; os comandos abaixo passam a valer
> quando o projeto React/Vite for criado.

```bash
cd frontend
npm install
npm run dev
```

---

## Banco de Dados

O projeto utiliza SQLite para desenvolvimento.

As migrations do Entity Framework Core são versionadas no repositório; o arquivo `.db` não —
ele é gerado localmente e está no `.gitignore`.

O `DbContext` vive na Infrastructure e o host é a Api, então os dois projetos precisam ser
informados em todo comando do `dotnet ef`. Os comandos abaixo rodam a partir de `backend/`:

Para criar uma nova migration:

```bash
dotnet ef migrations add NomeDaMigration \
  --project src/VaccinationControl.Infrastructure \
  --startup-project src/VaccinationControl.Api \
  --output-dir Persistence/Migrations
```

Para aplicar as migrations:

```bash
dotnet ef database update \
  --project src/VaccinationControl.Infrastructure \
  --startup-project src/VaccinationControl.Api
```

O caminho do banco é resolvido contra o content root da aplicação, não contra o diretório de
trabalho do processo — assim `dotnet run`, o executável em `bin/` e as ferramentas do EF
apontam sempre para o mesmo arquivo.

---

## Testes

Para executar todos os testes do backend:

```bash
dotnet test
```

Os testes são divididos entre:

* **Unit Tests** — validators, regras de domínio e handlers isolados
* **Integration Tests** — API completa via `WebApplicationFactory` sobre SQLite em memória

> Os dois projetos existem e estão ligados aos projetos de `src/`, mas ainda contêm apenas o
> teste de exemplo do template. A suíte real será escrita nas próximas etapas.

Para rodar um projeto ou um teste específico:

```bash
dotnet test tests/VaccinationControl.UnitTests
dotnet test --filter "FullyQualifiedName~NomeDoTeste"
```

---

## Git

O projeto utiliza Conventional Commits.

Exemplos:

```text
feat: add vaccination registration
fix: correct vaccination validation
refactor: improve vaccination service
test: add vaccination handler tests
docs: update README
chore: configure CI pipeline
```

### Branches

Branches devem seguir o padrão:

```text
feature/nome-da-feature
fix/nome-do-bug
refactor/nome-do-refactor
test/nome-do-teste
docs/nome-da-documentacao
chore/nome-da-tarefa
```

Exemplo:

```text
feature/vaccination-registration
```

---

## CI

O workflow [`.github/workflows/ci.yml`](.github/workflows/ci.yml) roda em Pull Requests e em
pushes para a `main`, e também pode ser disparado manualmente pela aba Actions.

O job de backend executa, em Release:

* Restore das dependências, com cache dos pacotes NuGet
* Build com `-warnaserror` — o projeto está em zero avisos e a pipeline mantém assim
* Testes automatizados

O job de frontend será adicionado quando `frontend/` tiver um `package.json`; hoje um
`npm ci` falharia e deixaria a pipeline vermelha sem haver nada errado no projeto.

---

## Licença

Este projeto está sob a licença MIT.
