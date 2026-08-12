# VaccinationControl

Sistema para gerenciamento de vacinação, permitindo o controle de pessoas, vacinas, aplicações e histórico de vacinação.

## Tecnologias

### Backend

* .NET 10
* ASP.NET Core
* Entity Framework Core
* SQLite
* MediatR (CQRS)
* FluentValidation
* xUnit, FluentAssertions e NSubstitute
* Scalar (documentação da API)

### Frontend

* React 19 + TypeScript
* Vite
* Material UI
* TanStack Query (estado de servidor)
* React Router
* React Hook Form
* Axios
* Vitest, Testing Library e MSW (testes de componente)

### DevOps

* GitHub Actions
* Git

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
| Application | `AddApplication()` | MediatR, `LoggingBehavior`, `ValidationBehavior`, validators do FluentValidation |
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
LoggingBehavior              registra o desfecho do caso de uso e quanto ele demorou
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

**Nenhum handler chama validator.** A validação acontece inteiramente no pipeline, antes dele
— se o request chegou ao handler, já está bem formado. O handler cuida apenas do que depende
do estado gravado.

Cada caso de uso tem um único validator, ao lado do seu command ou query.
todos registrados automaticamente por `AddValidatorsFromAssembly`.

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

### Logs

Toda requisição que passa pelo MediatR deixa **uma linha**, escrita pelo `LoggingBehavior` —
o behavior mais externo do pipeline, por isso ele enxerga também o que a validação barrou.
Log espalhado por handler divergiria no primeiro caso de uso novo; concentrado no pipeline,
o formato é o mesmo para os dezesseis endpoints, e um endpoint novo já nasce registrado.

| Desfecho | Nível | Linha |
| --- | --- | --- |
| Sucesso | `Information` | `CreateVaccineCommand concluido em 42 ms por <id>` |
| Entrada inválida (400) | `Warning` | `CreateVaccineCommand rejeitado na validacao de Name em 7 ms por <id>` |
| Regra de negócio (401/404/409/422) | `Warning` | `CreateVaccineCommand recusado por ConflictException em 4 ms por <id>` |
| Falha inesperada (500) | `Error` | registrada pelo `GlobalExceptionHandler`, com a exceção inteira |

Os campos são sempre os mesmos: caso de uso, desfecho, duração e autor — `anonimo` quando
não há sessão, como no login e no cadastro do primeiro usuário.

**O que não entra no log**, de propósito:

* **O corpo do request.** Ele carrega senha no login e CPF no cadastro de pessoa.
* **A mensagem da exceção.** Ela repete esses mesmos valores (*"Já existe uma pessoa
  cadastrada com o CPF '…'"*), então o motivo da recusa é o **tipo** da exceção. Quem precisa
  do detalhe tem a resposta HTTP, que o cliente recebeu.
* **Os valores dos campos inválidos.** Só os nomes, que bastam para saber qual formulário
  está mandando entrada errada.
* **Uma linha de início.** Ela dobraria o volume sem dizer nada que a de conclusão não diga.

A categoria é `VaccinationControl.UseCase`, então o volume se ajusta sem tocar em código:

```json
"Logging": { "LogLevel": { "VaccinationControl.UseCase": "Warning" } }
```

Duas categorias do framework já vêm baixadas em `appsettings.json`: `Microsoft.AspNetCore`
e `Microsoft.EntityFrameworkCore.Database.Command` — esta última despejaria o SQL de cada
consulta em `Information`, ruído que a linha de caso de uso já resume. Suba-a em
`appsettings.Development.json` quando precisar ver as queries.

Uma consequência a conhecer: requisição **sem sessão** não gera linha, porque a *fallback
policy* a barra antes do MediatR. Cookie vencido é rotina em qualquer aba aberta, e registrar
isso encheria o log sem informar nada. Tentativa de login com credencial errada **é**
registrada — ela chega ao handler e sai como `UnauthorizedException`.

---

## API

Documentação interativa disponível em desenvolvimento:

| Recurso | URL |
| --- | --- |
| Scalar (UI) | `http://localhost:5201/scalar/v1` |
| Documento OpenAPI | `http://localhost:5201/openapi/v1.json` |

### Autenticação

**Todos os endpoints exigem sessão**, exceto `register`, `login` e `logout`. A exigência é
aplicada por uma *fallback policy* global — um controller novo já nasce protegido, sem depender
de alguém lembrar de anotar `[Authorize]`.

O token **não aparece em nenhuma resposta**: o cadastro e o login o gravam no cookie
`vaccination-control-auth`, marcado `HttpOnly`, `Secure`, `SameSite=Lax` e com validade igual à
do token. É o navegador que o carrega dali em diante, e o JavaScript não tem acesso a ele — um
script injetado por XSS não consegue ler o que não pode ler.

```text
Cookie: vaccination-control-auth=<token>
```

Sem cookie, ou com token expirado, a resposta é `401`. O cabeçalho `Authorization: Bearer`
continua sendo aceito para quem não é navegador, mas o cookie tem precedência quando ambos vêm.

#### `POST /api/auth/register`

Cadastra um usuário e já abre a sessão — evita ter que chamar o login logo em seguida.

```json
{ "email": "admin@exemplo.com", "password": "senha12345" }
```

| HTTP | Quando |
| --- | --- |
| 201 | Usuário cadastrado |
| 400 | E-mail inválido, ou senha com menos de 8 caracteres |
| 409 | Já existe usuário com esse e-mail |

#### `POST /api/auth/login`

```json
{ "email": "admin@exemplo.com", "password": "senha12345" }
```

| HTTP | Quando |
| --- | --- |
| 200 | Autenticado |
| 400 | E-mail ou senha ausentes |
| 401 | Credencial inválida |

Ambos devolvem o mesmo corpo, e o `Set-Cookie` com o token:

```json
{
  "userId": "d2b32c2d-0970-4720-bd92-e3337ddc9089",
  "email": "admin@exemplo.com"
}
```

E-mail inexistente e senha errada devolvem **a mesma mensagem** — distinguir os dois
permitiria descobrir quais e-mails estão cadastrados.

#### `POST /api/auth/logout`

Apaga o cookie da sessão. Responde `204` sempre, inclusive sem sessão: quem chega com o cookie
já vencido continua precisando que ele seja apagado.

Precisa existir no servidor — apagar um cookie `HttpOnly` é impossível pelo JavaScript.

#### `GET /api/auth/me`

Devolve quem está autenticado na sessão corrente. É como a interface descobre se há sessão: o
cookie é `HttpOnly`, então não há nada no navegador que o JavaScript possa inspecionar.

| HTTP | Quando |
| --- | --- |
| 200 | Sessão válida |
| 401 | Sem sessão, sessão expirada, ou usuário que não existe mais |

```json
{
  "userId": "d2b32c2d-0970-4720-bd92-e3337ddc9089",
  "email": "admin@exemplo.com"
}
```

##### Senha

Nunca é gravada em claro. O hash usa o `PasswordHasher<T>` do ASP.NET Core, que aplica PBKDF2
com salt aleatório por senha e embute os parâmetros no próprio hash — trocar o custo no futuro
não invalida os hashes existentes.

##### Auditoria automática

Toda entidade gravada registra quem a criou ou alterou, a partir do `sub` do token:

| Campo | Quando é preenchido |
| --- | --- |
| `CreatedBy` | na inserção |
| `UpdatedBy` e `UpdatedAt` | na alteração |

O preenchimento acontece no `SaveChangesAsync` do `AppDbContext`, não em cada handler — a
origem do dado é sempre a mesma, e deixar isso a cargo dos handlers significaria esquecer em
algum. Em requisições anônimas (o cadastro do primeiro usuário) o autor fica vazio.

##### Chave de assinatura

`Jwt:Issuer`, `Jwt:Audience` e `Jwt:ExpiresInMinutes` ficam no `appsettings.json`. A **chave
não** — uma chave commitada fica no histórico do Git para sempre. Em desenvolvimento:

```bash
cd backend
dotnet user-secrets set "Jwt:Key" "<chave com no mínimo 32 bytes>" \
  --project src/VaccinationControl.Api
```

Em produção, use variável de ambiente. Sem a chave configurada a aplicação **não sobe**, com
mensagem explicando como defini-la — falhar no startup é melhor que rodar com uma chave padrão.

A seção `Jwt` é lida **uma única vez**, no `Program.cs`, e a mesma instância de `JwtSettings`
assina os tokens no `JwtTokenGenerator` e valida os recebidos no `AddJwtBearer`. Ler a chave em
dois pontos independentes permitiria que divergissem, e o sintoma seria a API rejeitar o token
que ela própria emitiu — sem nenhum erro apontando a causa.

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

#### `DELETE /api/vaccines/{id}`

Remove a vacina do catálogo. **Não há cascata aqui**: uma vacina com dose registrada não é
removida, porque o registro é o histórico de vacinação de alguém e não pode perder a vacina
que ele descreve. Para liberar a exclusão, remova antes os registros que a usam.

| HTTP | Quando |
| --- | --- |
| 204 | Vacina removida; sem corpo na resposta |
| 400 | Identificador vazio (`00000000-...`) |
| 404 | Não existe vacina com esse identificador |
| 409 | A vacina tem doses registradas |

### Pessoas

#### `POST /api/people`

Cadastra uma pessoa. O campo `document` é o **CPF**, sem máscara: exatamente 11 dígitos
(`^[0-9]{11}$`), e único entre as pessoas. As mensagens de erro chamam o campo de CPF, que é como a interface
o rotula — a propriedade continua `document`, e é essa a chave no dicionário de `errors`.

```json
{ "name": "Maria Silva", "document": "12345678901" }
```

| HTTP | Quando |
| --- | --- |
| 201 | Pessoa cadastrada; `Location` aponta para o recurso |
| 400 | Nome vazio ou acima de 200 caracteres; CPF que não seja 11 dígitos |
| 409 | Já existe pessoa com esse CPF |

```json
{
  "id": "94549402-7498-483b-b31a-da2c40d471ce",
  "name": "Maria Silva",
  "document": "12345678901"
}
```

#### `GET /api/people`

Lista as pessoas cadastradas, ordenadas por nome. Todos os parâmetros são opcionais — sem
nenhum deles, devolve todas.

| Parâmetro | Tipo | Descrição |
| --- | --- | --- |
| `search` | string | Filtra por trecho do **nome ou do CPF**, que está gravado sem máscara |
| `page` | int | Página desejada, a partir de 1. Padrão 1 |
| `pageSize` | int | Itens por página, de 1 a 100. Padrão 20 |

Mesmo envelope `PagedResult` da listagem de vacinas:

```json
{
  "items": [
    { "id": "99ea408a-…", "name": "Joao Pedro", "document": "11122233399" }
  ],
  "page": 1,
  "pageSize": 1,
  "totalCount": 1,
  "totalPages": 1
}
```

| HTTP | Quando |
| --- | --- |
| 200 | Consulta realizada |
| 400 | `page` menor que 1, `pageSize` fora de 1–100 ou `search` acima de 200 caracteres |

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

### Vacinações

Os registros ficam aninhados sob a pessoa, porque um registro de vacinação não existe
isolado — ele pertence ao cartão de alguém. O `personId` vem sempre da rota, nunca do corpo,
para que não haja duas fontes de verdade que possam divergir.

#### `POST /api/people/{personId}/vaccinations`

Registra uma vacinação no cartão da pessoa.

```json
{
  "vaccineId": "49eb0ff2-03c4-4601-9735-d7e00622c32e",
  "vaccinationType": "Dose",
  "doseNumber": 1,
  "vaccinationDate": "2024-02-10"
}
```

`vaccinationType` aceita `Dose` ou `BoosterDose` — enums trafegam como texto, para que o
cliente não precise conhecer a numeração interna do domínio.

| HTTP | Quando |
| --- | --- |
| 201 | Vacinação registrada; `Location` aponta para o recurso |
| 400 | Violação de RN01 ou RN02 |
| 404 | Pessoa ou vacina inexistente (RN03, RN04) |
| 409 | Violação de RN05, RN06, RN07 ou RN08 |

```json
{
  "id": "290c5a34-570e-46af-97e5-548ce265ac48",
  "personId": "c47da9ee-b418-49e4-8264-e8830e7913fd",
  "vaccineId": "49eb0ff2-03c4-4601-9735-d7e00622c32e",
  "vaccineName": "Hepatite B",
  "vaccinationType": "Dose",
  "doseNumber": 1,
  "vaccinationDate": "2024-02-10"
}
```

##### Regras da dose

| Regra | Descrição | Resposta |
| --- | --- | --- |
| RN01 | A dose precisa ser maior ou igual a 1 | 400 |
| RN02 | A data de aplicação não pode ser futura | 400 |
| RN03 | A pessoa precisa existir | 404 |
| RN04 | A vacina precisa existir | 404 |
| RN05 | A mesma dose **do mesmo tipo** não se repete para a pessoa e vacina | 409 |
| RN06 | A dose N exige a dose N−1 **do mesmo tipo** já registrada | 409 |
| RN07 | A dose N não pode ser anterior à data da dose N−1 **do mesmo tipo** | 409 |
| RN08 | Um reforço exige ao menos uma dose normal da mesma vacina | 409 |

RN01 e RN02 dependem só do formato e ficam no validator do command. As demais dependem do
estado já gravado, então vivem no handler — é o que permite distinguir 404 de 409, coisa que
um validator não faria, já que ele sempre resulta em 400.

##### Numeração independente por tipo

Doses normais e reforços têm **sequências próprias**. A dose normal 1 e a dose de reforço 1
são registros distintos e legítimos, e ter a dose normal 1 não habilita o reforço 2 — o
reforço 2 exige o reforço 1.

```text
Dose         1 ──> 2 ──> 3      sequência própria
BoosterDose  1 ──> 2            sequência própria, mas só começa
                                depois de existir alguma Dose (RN08)
```

RN05, RN06 e RN07 comparam apenas registros do mesmo tipo. RN08 é a única que cruza os dois:
ela fecha a brecha que as demais não pegam, já que a dose 1 de qualquer sequência não tem
antecessora para comparar — sem ela, o primeiro registro do cartão poderia entrar como
reforço.

O índice único do banco acompanha essa decisão e cobre
`(PersonId, VaccineId, VaccinationType, DoseNumber)`.

#### `GET /api/people/{personId}/vaccinations/{recordId}`

Consulta um registro do cartão. É o endereço devolvido no `Location` do registro.

O registro precisa pertencer à pessoa da rota: consultar um registro válido sob outro
`personId` devolve 404, e não os dados de outra pessoa.

| HTTP | Quando |
| --- | --- |
| 200 | Registro encontrado |
| 400 | Identificador vazio (`00000000-...`) |
| 404 | Registro inexistente ou pertencente a outra pessoa |

#### `DELETE /api/people/{personId}/vaccinations/{recordId}`

Remove um registro específico do cartão. O `recordId` vem da consulta do cartão.

| HTTP | Quando |
| --- | --- |
| 204 | Registro removido; sem corpo na resposta |
| 400 | Identificador vazio (`00000000-...`) |
| 404 | Registro inexistente ou pertencente a outra pessoa |

**A remoção é livre**: qualquer dose pode sair, inclusive do meio da sequência. Não há regra
de ordem — o endpoint é uma ferramenta de correção de lançamentos.

Isso permite que o cartão fique temporariamente com um buraco (dose 2 sem a dose 1) ou com
reforços sem dose inicial. Nenhum desses estados fica sem volta, porque as regras de registro
exigem apenas a dose imediatamente anterior:

| Estado após a remoção | Recriar a dose removida |
| --- | --- |
| Removida a dose 1, restam 2 e 3 | Permitido — a dose 1 não exige antecessora |
| Removida a dose 2, restam 1 e 3 | Permitido — a dose 1 existe, que é o que a RN06 pede |
| Removidas todas as normais, restam reforços | Permitido — dose normal 1 não exige nada |

As regras RN01 a RN08 descrevem o que pode ser **acrescentado** ao cartão, não um invariante
permanente dele.

### Cartão de vacinação

#### `GET /api/people/{personId}/vaccination-card`

Consulta o cartão de vacinação de uma pessoa, com as aplicações **agrupadas por vacina** — é
assim que um cartão se lê: o nome da vacina e as doses recebidas dela, não uma lista plana de
registros.

O cartão não é uma tabela: é a projeção dos registros de vacinação da pessoa. As vacinas vêm
em ordem alfabética e, dentro de cada uma, as doses vêm por tipo e número.

| HTTP | Quando |
| --- | --- |
| 200 | Cartão retornado, mesmo que vazio |
| 400 | Identificador vazio (`00000000-...`) |
| 404 | Não existe pessoa com esse identificador |

Uma pessoa sem nenhuma aplicação registrada devolve **200 com `vaccines` vazio**, não 404 —
a pessoa existe, o cartão dela é que está vazio.

```json
{
  "personId": "d1614d08-f31d-4da4-b654-544c66407697",
  "personName": "Maria Silva",
  "document": "33344455566",
  "vaccines": [
    {
      "vaccineId": "9016c829-3490-4164-90fc-5de8702bf760",
      "vaccineName": "Antitetanica",
      "totalDoses": 2,
      "doses": [
        {
          "recordId": "676c5a25-4947-484e-9dde-fd146bd65fdc",
          "vaccinationType": "Dose",
          "doseNumber": 1,
          "vaccinationDate": "2024-02-01"
        },
        {
          "recordId": "9ef33986-6685-4be3-b17b-eb2346f418f8",
          "vaccinationType": "Dose",
          "doseNumber": 2,
          "vaccinationDate": "2024-04-01"
        }
      ]
    },
    {
      "vaccineId": "7ad3501a-930a-4be1-8913-71b44d9069e8",
      "vaccineName": "Hepatite B",
      "totalDoses": 3,
      "doses": [
        {
          "recordId": "b9299d06-7393-47cd-9915-3839375a4ec5",
          "vaccinationType": "Dose",
          "doseNumber": 1,
          "vaccinationDate": "2024-01-10"
        },
        {
          "recordId": "03dea2e7-b5a5-48d5-a212-68240b9cacc5",
          "vaccinationType": "Dose",
          "doseNumber": 2,
          "vaccinationDate": "2024-03-10"
        },
        {
          "recordId": "eb9d533f-64f7-4791-b857-b616a2378b15",
          "vaccinationType": "BoosterDose",
          "doseNumber": 1,
          "vaccinationDate": "2024-09-10"
        }
      ]
    }
  ]
}
```

`totalDoses` conta todas as aplicações daquela vacina, somando doses normais e reforços.
O `recordId` de cada dose é o identificador usado para remover aquele registro específico.

### Exemplos de chamada

```bash
# 1. abrir a sessão — sem ela, todo o resto responde 401
#    o token vem no cookie, então basta guardar o cookie jar e reenviá-lo com -b
curl -s -c cookies.txt -X POST http://localhost:5201/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@exemplo.com","password":"senha12345"}'
```

Os comandos abaixo omitem o `-b cookies.txt` por brevidade; sem ele, a resposta é `401`.

```bash
# cadastrar uma vacina
curl -X POST http://localhost:5201/api/vaccines \
  -b cookies.txt \
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

# remover do catálogo; 409 se a vacina já tiver dose registrada
curl -X DELETE http://localhost:5201/api/vaccines/3df1340d-3381-4021-a782-18679e777c50
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

```bash
PESSOA=94549402-7498-483b-b31a-da2c40d471ce
VACINA=3df1340d-3381-4021-a782-18679e777c50

# registrar a primeira dose
curl -X POST "http://localhost:5201/api/people/$PESSOA/vaccinations" \
  -H "Content-Type: application/json" \
  -d "{\"vaccineId\":\"$VACINA\",\"vaccinationType\":\"Dose\",\"doseNumber\":1,\"vaccinationDate\":\"2024-02-10\"}"

# registrar o primeiro reforço — numeração própria, começa em 1
curl -X POST "http://localhost:5201/api/people/$PESSOA/vaccinations" \
  -H "Content-Type: application/json" \
  -d "{\"vaccineId\":\"$VACINA\",\"vaccinationType\":\"BoosterDose\",\"doseNumber\":1,\"vaccinationDate\":\"2024-05-10\"}"

# consultar um registro do cartão
curl "http://localhost:5201/api/people/$PESSOA/vaccinations/290c5a34-570e-46af-97e5-548ce265ac48"

# consultar o cartão completo, agrupado por vacina
curl "http://localhost:5201/api/people/$PESSOA/vaccination-card"

# remover um registro — na ordem inversa da aplicação
curl -X DELETE "http://localhost:5201/api/people/$PESSOA/vaccinations/290c5a34-570e-46af-97e5-548ce265ac48"
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
│   │   ├── VaccinationControl.UnitTests/        espelha as pastas da Application
│   │   └── VaccinationControl.IntegrationTests/ um arquivo por cenário
│   │
│   └── VaccinationControl.slnx
│
├── frontend/
│   ├── public/
│   ├── src/
│   │   ├── api/                               cliente axios, tipos do contrato, ProblemDetails
│   │   ├── auth/                              contexto de sessão e rota protegida
│   │   ├── components/                        AppLayout: barra, navegação e saída
│   │   ├── features/                          vaccines/ · people/ · vaccinationCard/ · auth/
│   │   ├── format/                            cpf.ts e date.ts, usados por mais de uma feature
│   │   ├── hooks/                             useDebouncedValue e useListQuery
│   │   ├── test/                              servidor MSW e helper de render
│   │   ├── App.tsx                            rotas
│   │   ├── main.tsx                           providers: tema, query client, router, sessão
│   │   ├── theme.ts                           tema do Material UI, locale ptBR
│   │   └── index.css
│   ├── .env.example                           VITE_API_URL
│   ├── eslint.config.js
│   ├── index.html
│   ├── package.json
│   └── vite.config.ts                         build e configuração do Vitest
│
├── .editorconfig
├── .gitignore
└── README.md
```

Dentro da Application, cada caso de uso tem a própria pasta com command/query, handler e
validator juntos:

```text
Application/Vaccinations/
├── VaccinationRecordResponse.cs
├── Commands/
│   ├── RegisterVaccination/           Request + Command + Handler + Validator
│   └── DeleteVaccinationRecord/       Command + Handler + Validator
└── Queries/
    ├── GetVaccinationRecordById/      Query + Handler + Validator
    └── GetVaccinationCard/            Query + Handler + Validator + 3 DTOs do cartão
```

O `RegisterVaccinationRequest` existe separado do command porque o `personId` vem da rota:
o command precisa dele para ser uma entrada completa do caso de uso, mas ele não deve
aparecer no corpo documentado pelo OpenAPI.

---

## Pré-requisitos

Backend:

* .NET 10 SDK
* Git
* `dotnet-ef` — necessário apenas para criar ou aplicar migrations:

```bash
dotnet tool install --global dotnet-ef
```

Frontend:

* Node.js 20.19+ (ou 22.12+) e npm — a faixa exigida pelo Vite 7

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

> A API precisa estar no ar em `http://localhost:5201`, e a origem `http://localhost:5173`
> precisa constar em `Cors:AllowedOrigins` — é o padrão do `appsettings.json`.

```bash
cd frontend
npm install
cp .env.example .env
npm run dev
```

A interface sobe em `http://localhost:5173`.

O `.env` guarda o endereço da API e **não é versionado** — o `.env.example` é o modelo:

```text
VITE_API_URL=http://localhost:5201
```

| Script | O que faz |
| --- | --- |
| `npm run dev` | servidor de desenvolvimento |
| `npm run build` | `tsc -b` e depois `vite build` |
| `npm run lint` | ESLint com verificação de tipos |
| `npm test` | Vitest, uma execução |
| `npm run test:watch` | Vitest em modo contínuo |

O `build` roda o `tsc` **antes** do Vite de propósito: o esbuild apaga as anotações de tipo
sem verificá-las, então sem essa etapa um erro de tipo passaria direto para o bundle.

### Telas

| Rota | Tela | Acesso |
| --- | --- | --- |
| `/login` | Entrar | público |
| `/registrar` | Criar conta | público |
| `/pessoas` | Lista de pessoas, com cadastro e exclusão | sessão |
| `/pessoas/:personId/cartao` | Cartão de vacinação, com registro e remoção de dose | sessão |
| `/vacinas` | Catálogo de vacinas, com busca, paginação, cadastro e exclusão | sessão |

A raiz redireciona para `/pessoas`. As rotas com sessão ficam atrás do `RequireSession`, que
consulta `GET /api/auth/me` na montagem — sem isso, um recarregamento de página piscaria a
tela de login antes do conteúdo.

### Organização

Por **feature**, espelhando a `Application/` do backend — mesma lógica de manter junto o que
muda junto:

```text
frontend/src/
├── api/          cliente axios, tipos do contrato e tradução de ProblemDetails
├── auth/         contexto de sessão e rota protegida
├── components/   AppLayout
├── features/     auth/ · vaccines/ · people/ · vaccinationCard/
├── format/       cpf.ts e date.ts
├── hooks/        useDebouncedValue e useListQuery
└── theme.ts
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

O projeto tem três suítes: unitários e de integração no backend, e testes de componente no
frontend.

Para executar todos os testes do backend, a partir de `backend/`:

```bash
dotnet test
```

Para rodar um projeto ou um teste específico:

```bash
dotnet test tests/VaccinationControl.UnitTests
dotnet test --filter "FullyQualifiedName~NomeDoTeste"
```

### Testes unitários

Cobrem a camada Application com **xUnit**, **FluentAssertions** e **NSubstitute**. Nenhum deles
toca banco: os repositórios são substituídos, e o que se verifica é a decisão do handler diante
do estado informado.

| Alvo | O que cobre |
| --- | --- |
| `RegisterVaccinationCommandHandler` | RN03 a RN08 — existência, duplicidade, sequência, ordem cronológica e reforço |
| Validators de command e query | RN01, RN02, limites de página e busca, e-mail e senha |
| `ValidationBehavior` | o mecanismo que torna a validação automática |
| Handlers de cadastro | conflito de nome e documento, remoção de espaços, gravação |
| `LoginCommandHandler` | credencial inválida e emissão do token |
| `GetVaccinationCardQueryHandler` | agrupamento por vacina, cartão vazio, `recordId` de cada dose |

A suíte cobre também o **caso negativo** que costuma passar despercebido: quando uma regra
falha, `Add` e `SaveChangesAsync` não podem ser chamados.

Dois comportamentos de segurança também têm teste: o login usa a mesma mensagem para e-mail
inexistente e senha errada, e o cadastro grava o hash e nunca a senha em claro.

Nomes de teste referenciam a regra (`RN06_deve_exigir_a_anterior_do_mesmo_tipo`), ligando a
suíte à tabela de [regras da dose](#regras-da-dose).

As pastas **espelham as de `Application/`**, então achar o teste de um arquivo é mecânico:
mesmo caminho, com o sufixo `Tests`.

```text
src/…/Application/Vaccines/Commands/CreateVaccine/CreateVaccineCommandHandler.cs
tests/VaccinationControl.UnitTests/Vaccines/Commands/CreateVaccine/CreateVaccineCommandHandlerTests.cs
```

### Testes de integração

Sobem a API inteira em memória com `WebApplicationFactory` e exercitam os endpoints por HTTP
real. Cobrem o que nenhum teste unitário alcança: persistência, cascata no banco, auditoria a
partir do token e tradução de exceção em status HTTP.

| Arquivo | O que cobre |
| --- | --- |
| `VaccinationFlowTests` | fluxo de ponta a ponta, RN03 a RN08 pela API, cascata da remoção |
| `AuthenticationTests` | 401 sem sessão, documentação anônima, cadastro e login |
| `SessionCookieTests` | marcas do cookie, ausência do token no corpo, `me` e `logout` |
| `CorsTests` | preflight liberado, credenciais permitidas, origem desconhecida recusada |
| `ListingTests` | busca, paginação, caixa e escape de curinga — tudo no SQL de verdade |
| `ErrorContractTests` | `application/problem+json`, dicionário de erros, rótulo em português |
| `AuditTests` | `CreatedBy` vindo do token, cadastro anônimo sem autor, senha nunca em claro |
| `OpenApiDocumentTests` | esquema de sessão no documento e o requisito só nas rotas protegidas |

Aqui as pastas **não** espelham o `src/`: cada arquivo é um cenário que atravessa vários casos
de uso, então agrupar por origem não ajudaria. O que fica separado é a infraestrutura, em
`Support/` — a factory e o cliente autenticado não são teste, são o que permite escrevê-los.

```text
tests/VaccinationControl.IntegrationTests/
├── Support/
│   ├── ApiFactory.cs      sobe a API com banco em memória
│   └── ApiClient.cs       cadastra um usuário e devolve o cliente autenticado
└── *Tests.cs              um arquivo por cenário
```

A `ApiFactory` troca só duas coisas do host real:

**Banco** — SQLite `:memory:` com a conexão mantida aberta pela vida do fixture; fechá-la
descarta o banco. Usa `MigrateAsync()` em vez de `EnsureCreated()`, então os testes também
provam que as migrations produzem um schema funcional, com índices únicos e `ON DELETE CASCADE`.

**Chave JWT** — via `UseSetting`, e não `ConfigureAppConfiguration`. O `Program.cs` lê a
configuração de JWT ao montar o builder, antes de qualquer fonte que a factory adicione depois;
uma chave que chegue tarde simplesmente não é usada.

O `ApiClient.AutenticadoAsync` cadastra um usuário com e-mail único e devolve um `HttpClient`
com a sessão aberta — sem ele, a *fallback policy* global responde 401 antes de qualquer
controller. Não há token a manipular: o `HttpClient` da factory guarda o cookie da resposta e o
reenvia sozinho, como o navegador faria.

O endereço base do cliente é `https://localhost`, e não HTTP. O cookie é `Secure`, e o
`CookieContainer` do .NET — ao contrário do navegador — não abre exceção para `localhost`: sobre
HTTP ele guardaria o cookie e nunca mais o enviaria, e todo teste autenticado responderia 401.

### Testes de frontend

**Vitest** com **Testing Library** para montar os componentes e **MSW** para simular a API.
A partir de `frontend/`:

```bash
npm test
npm run test:watch
```

O MSW é o que permite exercitar o caminho que mais quebra em silêncio: as respostas de erro.
Cada teste declara os *handlers* do seu cenário, e o `onUnhandledRequest: 'error'` derruba o
teste quando uma rota não simulada é chamada — em vez de deixar uma requisição real escapar
para a máquina de quem roda a suíte.

| Arquivo | O que cobre |
| --- | --- |
| `App.test.tsx` | registro das rotas, redirecionamentos e o desvio para o login sem sessão |
| `VaccinesPage` · `VaccineDialog` | busca, paginação de servidor, estados vazio e de erro, cadastro, exclusão |
| `DeleteVaccineDialog` | confirmação e o 409 da vacina em uso chegando à tela |
| `PeoplePage` · `PersonDialog` | máscara de CPF, envio sem máscara, busca por nome ou CPF |
| `DeletePersonDialog` | aviso de cascata com a contagem de registros que serão perdidos |
| `VaccinationCardPage` · `VaccinationDialog` | agrupamento por vacina, sugestões de dose, remoção |
| `nextDose` · `cpf` · `date` | as funções puras — sequência por tipo, máscara e datas sem fuso |

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

São dois jobs **independentes**, que rodam em paralelo — a falha de um não esconde o
resultado do outro.

O job de **backend** executa, em Release:

* Restore das dependências, com cache dos pacotes NuGet
* Build com `-warnaserror` — o projeto está em zero avisos e a pipeline mantém assim
* Testes automatizados

O job de **frontend** executa, com Node 22 e cache de npm chaveado pelo `package-lock.json`:

* `npm ci` — e não `install`: instala exatamente o que está no lock file e falha se ele
  estiver fora de sincronia com o `package.json`
* `npm run lint`
* `npm run build`
* `npm test`

---

## Licença

Este projeto está sob a licença MIT.
