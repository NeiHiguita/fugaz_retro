using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class PermisosFilterAttribute : ActionFilterAttribute
{
    private readonly string _permiso;

    public PermisosFilterAttribute(string permiso)
    {
        _permiso = permiso;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var session = context.HttpContext.Session;
        var permisos = session.GetString("Permisos")?.Split(',') ?? Array.Empty<string>();

        if (!permisos.Contains(_permiso))
        {
            context.Result = new ForbidResult();
        }

        base.OnActionExecuting(context);
    }
}
