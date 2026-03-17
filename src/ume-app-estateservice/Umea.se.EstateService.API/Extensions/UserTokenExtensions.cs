using Umea.se.Toolkit.UserFromToken;

namespace Umea.se.EstateService.API.Extensions;

public static class UserTokenExtensions
{
    public static string GetRequiredEmail(this UserToken userToken)
        => userToken.Email
           ?? throw new InvalidOperationException("No email claim found on the authenticated user.");
}
