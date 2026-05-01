using FluentValidation.Results;
using System.Net;

//======================================================================================================
// Pattern Result aplicado para encapsular o resultado de uma operação,
// seja ela bem-sucedida ou com falha.
//======================================================================================================
public sealed class Result<T>
{
    public T? Success { get; private set; }
    public Failure? Failure { get; private set; }
    public bool IsSuccess => Failure == null;
    public bool IsFailure => !IsSuccess;

    private Result(T success) { Success = success; }
    private Result(Failure failure) { Failure = failure; }
    public static Result<T> AsSuccess(T success) => new(success);
    public static Result<T> AsFailure(Failure failure) => new(failure);
}

//======================================================================================================
// Classe para representar os detalhes de uma falha,
// incluindo o código de status HTTP e uma lista de mensagens de erro.
//======================================================================================================
public sealed class Failure(HttpStatusCode statusCode, ValidationResult errors)
{
    public readonly HttpStatusCode StatusCode = statusCode;
    public readonly List<ValidationFailure> Errors = errors.Errors.ToList();
}