using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Db.Api.Mvc
{
    public class DeferredExecutionResult : ActionResult
    {
        private readonly Func<ActionContext, Task> _exec;

        public DeferredExecutionResult(Func<ActionContext, Task> exec)
        {
            _exec = exec;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;

            response.StatusCode = 200;
            response.ContentType = "application/json";

            await _exec(context);
        }
    }
}