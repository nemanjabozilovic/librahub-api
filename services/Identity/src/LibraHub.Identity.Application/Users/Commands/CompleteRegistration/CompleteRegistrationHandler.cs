using LibraHub.BuildingBlocks.Results;
using LibraHub.Identity.Application.Abstractions;
using MediatR;
using Error = LibraHub.BuildingBlocks.Results.Error;

namespace LibraHub.Identity.Application.Users.Commands.CompleteRegistration;

public class CompleteRegistrationHandler(
    IRegistrationCompletionTokenRepository tokenRepository,
    IUserRepository userRepository) : IRequestHandler<CompleteRegistrationCommand, Result>
{
    public async Task<Result> Handle(CompleteRegistrationCommand request, CancellationToken cancellationToken)
    {
        var token = await tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token == null || !token.IsValid)
        {
            return Result.Failure(Error.Validation("Invalid or expired registration completion token"));
        }

        var user = await userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user == null)
        {
            return Result.Failure(Error.NotFound("User not found"));
        }

        // Update user profile
        user.UpdateProfile(request.FirstName, request.LastName, request.Phone, request.DateOfBirth);

        // Mark token as used
        token.MarkAsUsed();

        await userRepository.UpdateAsync(user, cancellationToken);
        await tokenRepository.UpdateAsync(token, cancellationToken);

        return Result.Success();
    }
}

