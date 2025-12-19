using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RolixSAEProject.Helpers;

namespace RolixSAEProject.Filters;

public class CurrencyViewDataFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is Controller controller)
        {
            var cookie = context.HttpContext.Request.Cookies["Currency"];
            var currency = CurrencyHelper.Normalize(cookie);

            controller.ViewData["Currency"] = currency;
            controller.ViewData["CurrencySymbol"] = CurrencyHelper.Symbol(currency);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
