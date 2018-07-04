using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Admins.Helpers
{
    //public class AuthHandler : AuthorizationHandler<RoleRequirement>
    //{
    //    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
    //    {
    //        var user = context.User;

    //        if(!user.HasClaim(c => c.Type == "AuthRole"))
    //        {
    //            return Task.CompletedTask;
    //        }
    //        var role = user.FindFirst("AuthRole").Value;

    //        if(role != "")
    //        {
    //            return context.Succeed(requirement);
    //        }

    //        return Task.CompletedTask;
    //    }
    //}
}
