# Como Chamar a API SOAP do ERP-Lion - GetClientByOrder

Esta documentação explica como implementar chamadas SOAP para o serviço `GetClientByOrder` do ERP-Lion. As informações aqui podem ser aplicadas para outros métodos SOAP do mesmo serviço.

## Visão Geral

O serviço `GetClientByOrder` retorna informações do cliente (nome e e-mail) com base no ID do pedido. A comunicação é feita via SOAP 1.1 sobre HTTP POST.

## Endpoint

- **Base URL**: Configurável via `appsettings` (chave `EndPoints:LION`)
- **Caminho**: `/ws/SalesService.asmx`
- **URL completa**: `{BaseURL}/ws/SalesService.asmx`
- **Método HTTP**: POST

## Headers Necessários

### Content-Type
```
Content-Type: text/xml; charset=utf-8
```

### SOAPAction
```
SOAPAction: "http://tempuri.org/GetClientByOrder"
```

**Importante**: 
- O valor do SOAPAction deve estar entre aspas duplas
- O nome do método no SOAPAction deve corresponder exatamente ao nome do método no servidor SOAP
- Diferentes métodos SOAP podem ter diferentes nomes (ex: `updateOrderTrack`, `GetClientByOrder`)

## Estrutura do Envelope SOAP

O envelope SOAP deve seguir a estrutura padrão SOAP 1.1:

### Estrutura Básica
```xml
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <tns:GetClientByOrder xmlns:tns="http://tempuri.org/">
      <tns:orderId>{orderId}</tns:orderId>
    </tns:GetClientByOrder>
  </soap:Body>
</soap:Envelope>
```

### Parâmetros

- **orderId** (int): ID do pedido para consulta

### Namespace
- **Namespace do método**: `http://tempuri.org/`
- **Namespace SOAP**: `http://schemas.xmlsoap.org/soap/envelope/`
- **Prefix do método**: `tns`

## Resposta do Servidor

### Resposta de Sucesso

O servidor retorna um envelope SOAP contendo os dados em formato JSON ou XML:

```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <GetClientByOrderResponse xmlns="http://tempuri.org/">
      <GetClientByOrderResult>{"ClientName":"Nome do Cliente","Email":"email@exemplo.com"}</GetClientByOrderResult>
    </GetClientByOrderResponse>
  </soap:Body>
</soap:Envelope>
```

### Formato dos Dados

Os dados podem vir em dois formatos:

1. **JSON dentro do elemento Result**: A resposta JSON está contida dentro do elemento `{Metodo}Result` (ex: `GetClientByOrderResult`)
2. **Elementos XML individuais**: Os dados podem vir como elementos XML separados (`<ClientName>` e `<Email>`)

### Estrutura JSON Esperada
```json
{
  "ClientName": "Nome do Cliente",
  "Email": "email@exemplo.com",
  "Track": "código_rastreamento",
  "Via": "transportadora"
}
```

**Nota**: A API retorna também os campos `Track` e `Via`, mas esses campos não precisam ser mapeados no DTO se não forem utilizados. O deserializador JSON automaticamente ignora propriedades extras que não existem no DTO de destino.

### Resposta de Erro

Quando ocorre um erro, o servidor retorna um `soap:Fault`:

```xml
<soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <soap:Fault>
      <faultcode>soap:Client</faultcode>
      <faultstring>Mensagem de erro detalhada</faultstring>
      <detail />
    </soap:Fault>
  </soap:Body>
</soap:Envelope>
```

## Implementação Recomendada

### Passos para Implementar

1. **Configurar HttpClient**
   - Limpar headers padrão antes de adicionar novos
   - Definir Content-Type como `text/xml; charset=utf-8`
   - Adicionar header SOAPAction com o valor correto entre aspas

2. **Construir o Envelope SOAP**
   - Criar XML manualmente ou usar biblioteca de serialização SOAP
   - Incluir namespaces corretos
   - Escapar valores de parâmetros se necessário (especialmente caracteres especiais XML)

3. **Enviar Requisição**
   - Usar método POST
   - Enviar envelope SOAP como corpo da requisição
   - Verificar status code da resposta

4. **Processar Resposta**
   - Se status 200: Extrair dados do envelope SOAP
   - Se status diferente de 200 ou presença de `soap:Fault`: Tratar como erro

5. **Parsear Resposta**
   - Tentar extrair JSON do elemento `{Metodo}Result`
   - Se não houver JSON, tentar extrair elementos XML individuais
   - Usar regex ou parser XML conforme necessário

## Tratamento de Erros Comuns

### Erro: "Server did not recognize the value of HTTP Header SOAPAction"

**Causa**: O nome do método no SOAPAction não corresponde ao nome esperado pelo servidor.

**Solução**: 
- Verificar o nome exato do método no WSDL do serviço
- Comparar com outros métodos que funcionam (padrão pode ser case-sensitive)
- Confirmar se o método está implementado no servidor

### Erro: "The format of value 'text/xml; charset=utf-8' is invalid"

**Causa**: Formato incorreto do Content-Type ao criar StringContent.

**Solução**: 
- Definir encoding separadamente ao criar StringContent
- Usar MediaTypeHeaderValue para definir Content-Type após criar o content

### Resposta vazia ou dados não encontrados

**Causa**: O pedido não existe ou o formato da resposta é diferente do esperado.

**Solução**:
- Verificar se o orderId é válido
- Implementar parsing flexível que tente múltiplos formatos de resposta
- Adicionar logs detalhados da resposta para debug

## Boas Práticas

### Logging
- Registrar todos os parâmetros de entrada
- Registrar o envelope SOAP completo antes de enviar
- Registrar headers HTTP (especialmente SOAPAction)
- Registrar resposta completa do servidor (ou primeiros N caracteres)
- Registrar dados extraídos após parsing

### Validação
- Validar parâmetros de entrada antes de construir a requisição
- Tratar valores nulos ou vazios adequadamente
- Validar estrutura da resposta antes de tentar extrair dados

### Tratamento de Exceções
- Capturar exceções de rede (timeout, conexão)
- Capturar exceções de parsing (XML inválido, JSON inválido)
- Retornar DTO vazio ou null em caso de falha, dependendo da necessidade do negócio

### Configuração
- Armazenar endpoint base em configuração (appsettings, variáveis de ambiente)
- Não hardcodar URLs ou paths
- Facilitar mudanças entre ambientes (desenvolvimento, produção)

## Extrapolando para Outros Métodos SOAP

Para implementar outros métodos SOAP do mesmo serviço:

1. **Identificar o nome correto do método** (pode ser diferente do esperado)
2. **Verificar parâmetros necessários** no WSDL ou documentação
3. **Ajustar SOAPAction** para refletir o nome correto do método
4. **Ajustar envelope XML** para incluir os parâmetros corretos
5. **Ajustar parsing** conforme o formato de resposta esperado

### Exemplo: Método updateOrderTrack

O método `updateOrderTrack` usa:
- SOAPAction: `"http://tempuri.org/updateOrderTrack"` (minúsculas)
- Parâmetros: `orderId`, `track`, `via`
- Retorna: string XML simples

## Notas Importantes

- O serviço SOAP pode ser case-sensitive para nomes de métodos
- Alguns métodos podem usar minúsculas (ex: `updateOrderTrack`) e outros maiúsculas (ex: `GetClientByOrder`)
- Sempre verificar o WSDL do serviço ou testar no Postman antes de implementar
- A resposta pode variar entre JSON e XML, dependendo do método
- Implementar parsing flexível que tente múltiplos formatos

## Referências

- [SOAP 1.1 Specification](https://www.w3.org/TR/2000/NOTE-SOAP-20000508/)
- [HTTP Headers para SOAP](https://www.w3.org/TR/soap11/#_Toc478383490)
- WSDL do serviço: `{BaseURL}/ws/SalesService.asmx?wsdl`

