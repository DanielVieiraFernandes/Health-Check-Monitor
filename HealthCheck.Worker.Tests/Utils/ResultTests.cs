using System.Net;
using Xunit;
using FluentValidation.Results;

namespace HealthCheck.Tests.Utils;

public class ResultTests
{
    [Fact]
    public void AsSuccess_DeveTerIsSuccessTrue_E_IsFailureFalse()
    {
        var result = Result<string>.AsSuccess("ok");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("ok", result.Success);
        Assert.Null(result.Failure);
    }

    [Fact]
    public void AsFailure_DeveTerIsFailureTrue_E_IsSuccessFalse()
    {
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Campo", "Mensagem de erro")
        });

        var failure = new Failure(HttpStatusCode.BadRequest, validationResult);
        var result = Result<string>.AsFailure(failure);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Success);
        Assert.NotNull(result.Failure);
        Assert.Equal(HttpStatusCode.BadRequest, result.Failure!.StatusCode);
        Assert.Single(result.Failure.Errors);
        Assert.Equal("Mensagem de erro", result.Failure.Errors[0].ErrorMessage);
    }

    [Fact]
    public void AsFailure_ComMultiplosErros_DeveListarTodos()
    {
        var validationResult = new ValidationResult(new[]
        {
            new ValidationFailure("Nome", "Nome é obrigatório"),
            new ValidationFailure("Email", "Email inválido"),
            new ValidationFailure("Url", "URL deve ser HTTPS"),
        });

        var failure = new Failure(HttpStatusCode.UnprocessableEntity, validationResult);
        var result = Result<int>.AsFailure(failure);

        Assert.Equal(3, result.Failure!.Errors.Count);
        Assert.Contains(result.Failure.Errors, e => e.ErrorMessage == "URL deve ser HTTPS");
    }

    [Fact]
    public void Failure_ComStatusCodeDiferentes_DevePreservarStatusCode()
    {
        var statusCodes = new[]
        {
            HttpStatusCode.NotFound,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.Forbidden,
            HttpStatusCode.Conflict
        };

        foreach (var code in statusCodes)
        {
            var failure = new Failure(code, new ValidationResult());
            var result = Result<object>.AsFailure(failure);

            Assert.Equal(code, result.Failure!.StatusCode);
        }
    }
}
