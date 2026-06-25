using ErrorOr;
using FastEndpoints;
using GeneaPam.Api.Features.Auth.Emails;
using GeneaPam.Api.Features.Auth.Internal;
using GeneaPam.Api.Infrastructure.Http;
using GeneaPam.Api.Infrastructure.Jobs;
using GeneaPam.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace GeneaPam.Api.Features.Auth.Register;

public sealed class RegisterEndpoint(
    RegisterValidator validator,
    UserManager<ApplicationUser> userManager,
    IJobDispatcher jobDispatcher
) : Endpoint<RegisterRequest, RegisterResponse>
{
    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
        Tags("Auth");
        Description(b =>
            b.Produces<RegisterResponse>(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status409Conflict)
                .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        );
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var validated = await validator.ValidateToErrorOrAsync(req, ct);
        if (validated.IsError)
        {
            await HttpContext.Response.SendResultAsync(validated.Errors.ToProblemResult());
            return;
        }

        var result = await RegisterUserAsync(req, ct);

        await HttpContext.Response.SendResultAsync(
            result.MatchToResponse(response =>
                Results.Created($"/users/{response.UserId}", response)
            )
        );
    }

    private async Task<ErrorOr<RegisterResponse>> RegisterUserAsync(
        RegisterRequest request,
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
