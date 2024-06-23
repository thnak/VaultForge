using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Protector.Utils;

public static class AuthorizationPolicyExtensions
{
    public static IEnumerable<string> GetRoles(this AuthorizationPolicy policy)
    {
        var roleRequirements = policy.Requirements.OfType<RolesAuthorizationRequirement>().FirstOrDefault();
        return roleRequirements?.AllowedRoles ?? [];
    }

    public static async Task<IEnumerable<string>> GetRolesForPolicyAsync(this IAuthorizationPolicyProvider policyProvider, string policyName)
    {
        var policy = await policyProvider.GetPolicyAsync(policyName);
        return policy?.GetRoles() ?? [];
    }
}