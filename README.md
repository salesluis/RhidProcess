# RhidProcess

API ASP.NET que automatiza, com Puppeteer e Chrome headless, o desbloqueio de
equipamentos no portal RHID.

## Endpoints

### Desbloquear um equipamento

```http
GET /v2/rhid/unlock?serial=SERIAL&password=SENHA
X-Api-Key: CHAVE_DA_API
```

Em caso de sucesso, a API preserva o contrato existente:

```json
{
  "contraSenha": "resultado"
}
```

> **Atenção:** este endpoint continua usando `GET` por compatibilidade. A senha
> do equipamento fica na URL e pode ser registrada por navegador, proxy,
> balanceador ou ferramenta de observabilidade, mesmo que os logs internos da
> aplicação sejam sanitizados. Evite reutilizar senhas e restrinja a retenção e
> o acesso aos logs da infraestrutura.

Exemplo local:

```bash
curl \
  -H "X-Api-Key: CHAVE_DA_API" \
  "http://localhost:4563/v2/rhid/unlock?serial=SERIAL&password=SENHA"
```

### Verificar a saúde

```http
GET /v2/health
X-Api-Key: CHAVE_DA_API
```

O health check valida somente a configuração local obrigatória e a existência
do executável do Chrome. Ele não autentica nem faz requisição ao portal RHID.

- `200 OK` com `{"status":"healthy"}`: configuração local pronta para uso.
- `503 Service Unavailable`: resposta no contrato de erro, sem revelar qual
  configuração está ausente ou inválida.

## Configuração

Copie `.env.example` para `.env`, substitua todos os valores de exemplo e inicie:

```bash
cp .env.example .env
docker compose up --build
```

As opções `Rhid__...` seguem a convenção de configuração hierárquica do
ASP.NET (`__` representa `:`):

| Variável | Finalidade |
| --- | --- |
| `API_KEY` | Chave exigida no header `X-Api-Key`. |
| `Rhid__BaseUrl` | Origem HTTPS do portal RHID, sem credenciais. |
| `Rhid__Email` | Conta de serviço usada no login do portal. |
| `Rhid__Password` | Senha da conta de serviço. |
| `Rhid__LoginRoute` | Rota da página de login. |
| `Rhid__UnlockRoute` | Rota da página de desbloqueio. |
| `Rhid__NavigationTimeoutSeconds` | Limite para navegações do browser. |
| `Rhid__ActionTimeoutSeconds` | Limite para ações e seletores do browser. |
| `PUPPETEER_EXECUTABLE_PATH` | Caminho absoluto para o Chrome. |
| `LOGS_PATH` | Diretório de logs no host. |

Não grave `.env`, credenciais ou chaves reais no repositório. Credenciais RHID
que já tenham aparecido no histórico Git devem ser consideradas comprometidas e
rotacionadas; removê-las apenas da versão atual não invalida cópias anteriores.

## Diagnóstico de erros

Falhas da automação retornam um identificador correlacionável, sem stack trace
ou dados sensíveis:

```json
{
  "errorId": "identificador-da-falha",
  "code": "RHID_LOGIN_NOT_CONFIRMED",
  "stage": "login_submit",
  "message": "Não foi possível confirmar o login no RHID."
}
```

Principais códigos:

| Código | HTTP | Significado |
| --- | ---: | --- |
| `RHID_CONFIGURATION_INVALID` | 503 | Configuração local ou Chrome inválido. |
| `RHID_UPSTREAM_TIMEOUT` | 504 | O portal não respondeu dentro do timeout. |
| `RHID_LOGIN_NOT_CONFIRMED` | 502 | O envio ocorreu, mas o login não foi confirmado. |
| `RHID_UPSTREAM_FAILURE` | 502 | Resposta ou comportamento inesperado do portal. |
| `INTERNAL_ERROR` | 500 | Falha interna não classificada. |

O campo `stage` localiza a etapa: `configuration`, `browser_startup`,
`login_page`, `login_submit`, `unlock_page`, `unlock_submit` ou `result_read`.

No Docker Compose, os arquivos ficam em `${LOGS_PATH:-./Logs}`. Use o `errorId`
recebido pela API para localizar o registro correspondente:

```bash
rg --fixed-strings "IDENTIFICADOR_RECEBIDO" Logs/
```

Os logs internos omitem query string, credenciais, API key, serial completo,
HTML e screenshots. Eventos do portal registram somente dados operacionais
sanitizados, como etapa, duração, tipo da exceção e status HTTP.

## Desenvolvimento

Para compilar e executar os testes sem acessar o portal real:

```bash
dotnet build RhidProcess.slnx
dotnet test RhidProcess.slnx
```
