# Estratégia de Testes

## Visão geral

Testes unitários com **xUnit** + **Moq**. Localizados em `HealthCheck.Worker.Tests/`.

- **Framework**: xUnit
- **Mocking**: Moq
- **Target**: .NET 10
- **Padrão**: TDD (Red-Green-Refactor)

## Estrutura de testes

```
HealthCheck.Worker.Tests/
├── Utils/
│   └── ResultTests.cs                        # Padrão Result<T>
├── Models/
│   └── WorkerConfigTests.cs                  # Valores padrão, ranges
├── Validators/
│   ├── CreateMonitoredSystemValidatorTests.cs # Validação de entrada
│   └── WorkerConfigValidatorTests.cs         # Validação de ranges
├── Cryptography/
│   └── BcryptPasswordEncrypterTests.cs       # Hash/verify, salt aleatório
└── Worker/
    └── GetExceptionNameTests.cs              # Timeout vs cancelamento
```

## Executando testes

```bash
# Todos os testes
dotnet test HealthCheck.Worker.Tests

# Teste específico
dotnet test HealthCheck.Worker.Tests --filter "FullyQualifiedName~ResultTests"

# Verboso
dotnet test HealthCheck.Worker.Tests --verbosity normal
```

## Convenções de nomenclatura

- **Classes**: sufixo `Tests` (ex: `ResultTests`)
- **Métodos**: `[Fact]` ou `[Theory]` com nomes descritivos em português
- **Nomes descrevem comportamento**: `Deve_RetornarFalha_QuandoNomeVazio`

## Padrão Result<T>

O padrão `Result<T>` é extensivamente testado:

- `AsSuccess` retorna objeto com `IsSuccess = true`
- `AsFailure` retorna erros com `IsFailure = true`
- `AsFailure` aceita múltiplos `ValidationFailure`
- Status code é propagado corretamente

## Testando validadores FluentValidation

```csharp
[Fact]
public async Task Deve_Falhar_Quando_Nome_Vazio()
{
    var dto = new CreateMonitoredSystemDTO { Name = "", Url = "https://..." };
    var result = await _validator.TestValidateAsync(dto);
    result.ShouldHaveValidationErrorFor(x => x.Name);
}
```

## Testando bcrypt

```csharp
[Fact]
public void Deve_Verificar_Senha_Correta()
{
    var hash = _encrypter.Encrypt("123456");
    var result = _encrypter.Compare("123456", hash);
    Assert.True(result);
}

[Fact]
public void Deve_Lancar_Excecao_Com_Hash_Vazio()
{
    Assert.Throws<ArgumentException>(() =>
        _encrypter.Compare("123456", ""));
}
```

## Pipeline de testes

```
1. dotnet test     → Testes unitários
2. dotnet build    → Verificação de compilação
3. Visual          → Verificação manual de UI (responsabilidade do desenvolvedor)
4. Commit          → Se tudo passou
```

## Pitfalls

- **Async validation**: validadores com `MustAsync` precisam de `TestValidateAsync()` nos testes
- **InternalsVisibleTo**: `HealthCheck.Framework.csproj` já tem `[InternalsVisibleTo]` para o projeto de testes
- **dotnet test travando**: matar processos com `taskkill /F /IM dotnet.exe` e tentar novamente
- **Testes com DB**: testes atuais são puramente unitários. Se adicionar testes de integração, configurar connection string de teste separada
