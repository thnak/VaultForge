using System.Security.Claims;
using BusinessModels.Resources;
using BusinessModels.Utils;
using Microsoft.AspNetCore.Authorization;

namespace Business.Authenticate.AuthorizationRequirement;

public class OverYearOldRequirement(int old) : AuthorizationHandler<OverYearOldRequirement>, IAuthorizationRequirement
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OverYearOldRequirement requirement)
    {
        if (!context.User.HasClaim(c => c.Type == ClaimTypes.DateOfBirth))
        {
            context.Fail(new AuthorizationFailureReason(this, AppLang.This_resource_requires__0__years_old.AutoReplace([$"{old}"])));
            return Task.CompletedTask;
        }

        var dobVal = context.User.FindFirst(c => c.Type == ClaimTypes.DateOfBirth)?.Value;
        if (string.IsNullOrEmpty(dobVal))
        {
            context.Fail(new AuthorizationFailureReason(this, AppLang.This_resource_requires__0__years_old.AutoReplace([$"{old}"])));
        }
        else
        {
            var dateOfBirth = Convert.ToDateTime(dobVal);
            var age = DateTime.Today.Year - dateOfBirth.Year;
            if (dateOfBirth > DateTime.Today.AddYears(-age))
            {
                age--;
            }

            if (age >= old)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail(new AuthorizationFailureReason(this, AppLang.This_resource_requires__0__years_old.AutoReplace($"{old}")));
            }
        }
        return Task.CompletedTask;
    }
}