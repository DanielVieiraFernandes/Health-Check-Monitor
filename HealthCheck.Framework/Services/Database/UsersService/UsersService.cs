using FluentValidation.Results;
using HealthCheck.Framework.Models;
using HealthCheck.Framework.Repositories.UsersRepository;
using HealthCheck.Framework.Services.Cryptography;
using HealthCheck.Framework.Services.Database.UsersService.Validators;
using System.Net;

namespace HealthCheck.Framework.Services.Database.UsersService;

public class UsersService(IUsersRepository usersRepository,
                          IPasswordEncrypter passwordEncrypter)
{
    public async Task<Result<User>> CreateUser(User user)
    {
        CreateUserValidator validator = new();

        var validationResult = validator.Validate(user);

        if (!validationResult.IsValid)
        {
            Failure failure = new(HttpStatusCode.BadRequest, validationResult);

            return Result<User>.AsFailure(failure);
        }

        user.Password = passwordEncrypter.Encrypt(user.Password);

        var userCreated = await usersRepository.Create(user);

        //-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // Se por acaso o repositório retornar null, isso indica que houve um erro ao criar o usuário,
        // então retornamos uma falha genérica.
        //-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        if (userCreated == null)
        {
            Failure failure = new(HttpStatusCode.InternalServerError, new ValidationResult { Errors = { new ValidationFailure("User", "Failed to create user") } });
            return Result<User>.AsFailure(failure);
        }

        return Result<User>.AsSuccess(userCreated);
    }

    public async Task<Result<User?>> GetUserById(Guid id)
    {
        var user = await usersRepository.GetById(id);

        if (user == null)
            return Result<User?>.AsSuccess(null);

        return Result<User?>.AsSuccess(user);
    }

    public async Task<Result<User?>> GetUserByEmail(string email)
    {
        var user = await usersRepository.GetByEmail(email);

        if (user == null)
            return Result<User?>.AsSuccess(null);

        return Result<User?>.AsSuccess(user);
    }

    //------------------------------------------------------------------------------------------------------
    // Autentica o usuário verificando email e senha.
    // Retorna sucesso com o usuário encontrado ou falha com mensagem descritiva.
    //------------------------------------------------------------------------------------------------------
    public async Task<Result<User>> AuthenticateUser(string email, string password)
    {
        //==============================================================================================
        // 1) Busca o usuário por email.
        // 2) Compara a senha informada com o hash armazenado.
        //==============================================================================================
        var user = await usersRepository.GetByEmail(email);

        if (user == null)
        {
            Failure failure = new(HttpStatusCode.Unauthorized, new ValidationResult
            {
                Errors = { new ValidationFailure("User", "Credenciais inválidas") }
            });

            return Result<User>.AsFailure(failure);
        }

        if (!passwordEncrypter.Compare(password, user.Password))
        {
            //------------------------------------------------------------------------------------------
            // Se a senha não bater, não informamos o motivo exato para evitar exposição.
            //------------------------------------------------------------------------------------------
            Failure failure = new(HttpStatusCode.Unauthorized, new ValidationResult
            {
                Errors = { new ValidationFailure("User", "Credenciais inválidas") }
            });

            return Result<User>.AsFailure(failure);
        }

        return Result<User>.AsSuccess(user);
    }
}
