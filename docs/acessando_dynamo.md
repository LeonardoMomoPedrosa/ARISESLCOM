# Acessando DynamoDB no SITECOM

Este documento explica como o projeto SITECOM acessa o Amazon DynamoDB, permitindo que outros projetos (como SLCOM) implementem o mesmo padrão.

## Visão Geral

O SITECOM utiliza o AWS SDK para .NET (`AWSSDK.DynamoDBv2`) para acessar o DynamoDB. O cliente DynamoDB é configurado como um serviço singleton no container de injeção de dependência e pode ser injetado em qualquer serviço ou controller que precise acessar o DynamoDB.

## 1. Pacote NuGet Necessário

Adicione o pacote AWS SDK DynamoDB ao seu projeto `.csproj`:

```xml
<PackageReference Include="AWSSDK.DynamoDBv2" Version="3.7.*" />
```

Ou via NuGet Package Manager:
```
Install-Package AWSSDK.DynamoDBv2
```

## 2. Configuração no appsettings.json

Configure as credenciais e informações do DynamoDB no arquivo `appsettings.json`:

```json
{
  "AWS": {
    "PersonalizeDynamo": {
      "TableName": "dynamo-personalize",
      "Region": "us-east-1",
      "AccessKey": "SUA_ACCESS_KEY_AQUI",
      "SecretKey": "SUA_SECRET_KEY_AQUI"
    },
    "TrackerPedidos": {
      "TableName": "tracker-pedidos",
      "Region": "us-east-1",
      "AccessKey": "SUA_ACCESS_KEY_AQUI",
      "SecretKey": "SUA_SECRET_KEY_AQUI"
    }
  }
}
```

### Configuração por Ambiente

Para diferentes ambientes (Development, Production), você pode sobrescrever essas configurações nos arquivos:
- `appsettings.Development.json`
- `appsettings.Production.json`

**Importante:** Em produção, considere usar variáveis de ambiente ou AWS IAM Roles ao invés de hardcoded credentials.

## 3. Configuração no Program.cs (Injeção de Dependência)

Registre o cliente DynamoDB como um serviço singleton no `Program.cs`:

```csharp
using Amazon.DynamoDBv2;
using Amazon;

builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var region = configuration["AWS:PersonalizeDynamo:Region"];
    var accessKey = configuration["AWS:PersonalizeDynamo:AccessKey"];
    var secretKey = configuration["AWS:PersonalizeDynamo:SecretKey"];

    var dynamoConfig = new AmazonDynamoDBConfig();
    if (!string.IsNullOrWhiteSpace(region))
    {
        dynamoConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
    }

    if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
    {
        return new AmazonDynamoDBClient(accessKey, secretKey, dynamoConfig);
    }

    // Se não houver credenciais, usa credenciais padrão do ambiente (IAM Role, etc.)
    return new AmazonDynamoDBClient(dynamoConfig);
});
```

### Explicação da Configuração

- **Singleton**: O cliente DynamoDB é registrado como singleton para reutilização e eficiência
- **Region**: Define a região AWS onde a tabela está localizada (ex: `us-east-1`)
- **Credenciais**: Se `AccessKey` e `SecretKey` estiverem configurados, usa credenciais explícitas. Caso contrário, usa credenciais padrão do ambiente (útil para EC2 com IAM Roles)

## 4. Usando o Cliente DynamoDB

### Injeção em um Service

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

public class MeuService
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IConfiguration _configuration;

    public MeuService(
        IAmazonDynamoDB dynamoDb,
        IConfiguration configuration)
    {
        _dynamoDb = dynamoDb;
        _configuration = configuration;
    }

    // Métodos que usam o DynamoDB...
}
```

### Injeção em um Controller

```csharp
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

[ApiController]
[Route("api/[controller]")]
public class MeuController : ControllerBase
{
    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly IConfiguration _configuration;

    public MeuController(
        IAmazonDynamoDB dynamoDb,
        IConfiguration configuration)
    {
        _dynamoDb = dynamoDb;
        _configuration = configuration;
    }

    // Endpoints que usam o DynamoDB...
}
```

## 5. Exemplos de Operações

### Ler um Item (GetItem)

```csharp
public async Task<string?> GetItemFromDynamoAsync(string chave)
{
    try
    {
        var tableName = _configuration["AWS:PersonalizeDynamo:TableName"];
        
        var request = new GetItemRequest
        {
            TableName = tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                { "productId", new AttributeValue { S = chave } }
            }
        };

        var response = await _dynamoDb.GetItemAsync(request);

        if (response.Item == null || response.Item.Count == 0)
        {
            return null;
        }

        // Ler um atributo específico (ex: "data")
        if (response.Item.TryGetValue("data", out var dataAttribute))
        {
            return dataAttribute.S; // Retorna como string
        }

        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao ler item do DynamoDB para chave {Chave}", chave);
        throw;
    }
}
```

### Salvar um Item (PutItem)

```csharp
public async Task SaveItemToDynamoAsync(string chave, string json)
{
    try
    {
        var tableName = _configuration["AWS:PersonalizeDynamo:TableName"];
        
        var request = new PutItemRequest
        {
            TableName = tableName,
            Item = new Dictionary<string, AttributeValue>
            {
                { "productId", new AttributeValue { S = chave } },
                { "data", new AttributeValue { S = json } },
                { "updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } }
            }
        };

        await _dynamoDb.PutItemAsync(request);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao salvar item no DynamoDB para chave {Chave}", chave);
        throw;
    }
}
```

### Descrever uma Tabela (DescribeTable)

```csharp
public async Task<string> GetPrimaryKeyNameAsync(string tableName)
{
    try
    {
        var tableDescription = await _dynamoDb.DescribeTableAsync(new DescribeTableRequest
        {
            TableName = tableName
        });

        var keySchema = tableDescription.Table.KeySchema;
        var partitionKey = keySchema.FirstOrDefault(k => k.KeyType == KeyType.HASH)?.AttributeName;
        
        return partitionKey ?? "id"; // Retorna o nome da chave primária
    }
    catch (ResourceNotFoundException)
    {
        throw new InvalidOperationException($"A tabela DynamoDB '{tableName}' não existe.");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erro ao obter descrição da tabela DynamoDB {TableName}", tableName);
        throw;
    }
}
```

## 6. Tipos de Dados DynamoDB

O DynamoDB usa `AttributeValue` para representar diferentes tipos de dados:

- **String**: `new AttributeValue { S = "valor" }`
- **Number**: `new AttributeValue { N = "123" }` (sempre como string)
- **Boolean**: `new AttributeValue { BOOL = true }`
- **List**: `new AttributeValue { L = new List<AttributeValue> { ... } }`
- **Map**: `new AttributeValue { M = new Dictionary<string, AttributeValue> { ... } }`

### Exemplo com Tipos Mistos

```csharp
var item = new Dictionary<string, AttributeValue>
{
    { "id", new AttributeValue { S = "123" } },
    { "quantidade", new AttributeValue { N = "10" } },
    { "ativo", new AttributeValue { BOOL = true } },
    { "tags", new AttributeValue 
        { 
            L = new List<AttributeValue> 
            { 
                new AttributeValue { S = "tag1" },
                new AttributeValue { S = "tag2" }
            } 
        } 
    }
};
```

## 7. Tratamento de Erros

### Erros Comuns

- **ResourceNotFoundException**: Tabela não existe
- **ProvisionedThroughputExceededException**: Limite de throughput excedido
- **ValidationException**: Dados inválidos na requisição
- **AmazonDynamoDBException**: Erro genérico do DynamoDB

### Exemplo de Tratamento

```csharp
try
{
    var response = await _dynamoDb.GetItemAsync(request);
    // Processar resposta...
}
catch (ResourceNotFoundException ex)
{
    _logger.LogError(ex, "Tabela {TableName} não encontrada", tableName);
    // Retornar null ou lançar exceção customizada
    return null;
}
catch (AmazonDynamoDBException ex)
{
    _logger.LogError(ex, "Erro do DynamoDB: {Message}", ex.Message);
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Erro inesperado ao acessar DynamoDB");
    throw;
}
```

## 8. Exemplos Reais do SITECOM

### PersonalizeService.cs

O `PersonalizeService` usa o DynamoDB para buscar recomendações de produtos:

```csharp
private async Task<List<string>> FetchFromDynamoAsync(string itemId)
{
    var tableName = _configuration["AWS:PersonalizeDynamo:TableName"];
    var request = new GetItemRequest
    {
        TableName = tableName,
        Key = new Dictionary<string, AttributeValue>
        {
            { "productId", new AttributeValue { S = itemId } }
        }
    };

    var response = await _dynamoDb.GetItemAsync(request);
    // Processar resposta...
}
```

### WebhookController.cs

O `WebhookController` usa o DynamoDB para salvar dados de rastreamento de pedidos:

```csharp
private async Task SaveItemToDynamoAsync(string chave, string json)
{
    var tableName = GetTableName();
    var primaryKeyName = await GetPrimaryKeyNameAsync(tableName);
    
    var request = new PutItemRequest
    {
        TableName = tableName,
        Item = new Dictionary<string, AttributeValue>
        {
            { primaryKeyName, new AttributeValue { S = chave } },
            { "rastreamento_json", new AttributeValue { S = json } },
            { "updated_at", new AttributeValue { S = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") } }
        }
    };

    await _dynamoDb.PutItemAsync(request);
}
```

## 9. Boas Práticas

1. **Cache de Configurações**: Cache o nome da chave primária e outras informações da tabela para evitar chamadas repetidas ao `DescribeTable`
2. **Tratamento de Erros**: Sempre trate exceções específicas do DynamoDB
3. **Logging**: Use logging adequado para facilitar debugging
4. **Credenciais**: Em produção, use IAM Roles ao invés de credenciais hardcoded
5. **Região**: Certifique-se de usar a região correta onde sua tabela está localizada
6. **Singleton**: O cliente DynamoDB é thread-safe e deve ser usado como singleton

## 10. Checklist para Implementação no SLCOM

- [ ] Adicionar pacote `AWSSDK.DynamoDBv2` ao projeto
- [ ] Configurar seção `AWS` no `appsettings.json` com credenciais e região
- [ ] Registrar `IAmazonDynamoDB` como singleton no `Program.cs`
- [ ] Injetar `IAmazonDynamoDB` nos serviços/controllers que precisam
- [ ] Implementar métodos de acesso ao DynamoDB (GetItem, PutItem, etc.)
- [ ] Adicionar tratamento de erros adequado
- [ ] Configurar logging para operações DynamoDB
- [ ] Testar conexão e operações básicas

## Referências

- [AWS SDK for .NET - DynamoDB](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/dynamodb-intro.html)
- [DynamoDB API Reference](https://docs.aws.amazon.com/amazondynamodb/latest/APIReference/Welcome.html)
- [Best Practices for DynamoDB](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/best-practices.html)

