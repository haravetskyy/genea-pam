using ErrorOr;
using FluentValidation;
using GeneaPam.Api.Infrastructure.Email;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Jobs;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace GeneaPam.Api.Features.Auth;

public sealed class RegisterEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", HandleAsync)
            .AllowAnonymous()
            .WithTags("Auth")
            .Produces<RegisterResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    internal static async Task<IResult> HandleAsync(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        UserManager<ApplicationUser> userManager,
        IJobDispatcher jobDispatcher,
        CancellationToken cancellationToken
    )
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation
                .Errors.Select(f =>
                    f.ErrorCode switch
                    {
                        var c when c == AuthErrors.PasswordTooShort.Code =>
                            AuthErrors.PasswordTooShort,
                        var c when c == AuthErrors.PasswordBreached.Code =>
                            AuthErrors.PasswordBreached,
                        var c when c == AuthErrors.EmailDisposable.Code =>
                            AuthErrors.EmailDisposable,
                        _ => AuthErrors.EmailInvalid,
                    }
                )
                .ToList();
            return errors.ToProblemResult();
        }

        var result = await RegisterUserAsync(
            request,
            userManager,
            jobDispatcher,
            cancellationToken
        );
        return result.MatchToResponse(response =>
            Results.Created($"/users/{response.UserId}", response)
        );
    }

    private static async Task<ErrorOr<RegisterResponse>> RegisterUserAsync(
        RegisterRequest request,
        UserManager<ApplicationUser> userManager,
        IJobDispatcher jobDispatcher,
        CancellationToken cancellationToken
    )
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return AuthErrors.EmailAlreadyTaken;

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var identityResult = await userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            // Identity password rules already enforced by validator; surface as generic conflict
            return Error.Failure(
                code: "Auth.RegistrationFailed",
                description: identityResult.Errors.First().Description
            );
        }

        await jobDispatcher.SendAsync(
            new WelcomeEmailJob(
                To: user.Email!,
                UserName: user.DisplayName,
                LanguagePreference: user.LanguagePreference
            ),
            cancellationToken
        );

        return new RegisterResponse(UserId: user.Id, DisplayName: user.DisplayName);
    }
}
