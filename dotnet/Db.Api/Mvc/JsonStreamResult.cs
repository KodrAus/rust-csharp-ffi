using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Db.Api.Mvc
{
    class JsonStreamResult : ActionResult
    {
        private readonly Func<Stream, Task> _write;

        public JsonStreamResult(Func<Stream, Task> write)
        {
            _write = write;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var response = context.HttpContext.Response;

            response.StatusCode = 200;
            response.ContentType = "application/json";

            await _write(response.Body);
        }
    }
}