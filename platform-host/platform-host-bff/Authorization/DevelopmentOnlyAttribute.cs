using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PlatformBff.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DevelopmentOnlyAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var environment = context.HttpContext.RequestServices
            .GetRequiredService<IHostEnvironment>();

        if (!environment.IsDevelopment())
        {
            context.Result = new ObjectResult(new
            {
                error = "This endpoint is only available in development environment",
                status = 403
            })
            {
                StatusCode = 403
            };
        }

        base.OnActionExecuting(context);
    }
}