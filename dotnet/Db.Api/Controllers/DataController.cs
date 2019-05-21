using System.Text.Json;
using System.Threading.Tasks;
using Db.Api.Mvc;
using Db.Api.Storage;
using Db.Storage;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Db.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly DataStore _store;

        public DataController(DataStore store)
        {
            _store = store;
        }

        [HttpGet]
        public ActionResult Get()
        {
            // NOTE: We need this for now since `Utf8JsonWriter` will
            // unconditionally issue a synchronous `Flush`, even if there's
            // no data. Once we can call `DisposeAsync` this can go away
            AllowSynchronousIO();

            var outerReader = _store.BeginRead();
            return new JsonStreamResult(async body =>
            {
                using var reader = outerReader;
                using var writer = new Utf8JsonWriter(body);

                writer.WriteStartArray();
                foreach (var outerData in reader.Data())
                {
                    using var data = outerData;
                    data.WriteAsValue(writer);
                }

                writer.WriteEndArray();

                await writer.FlushAsync(HttpContext.RequestAborted);
            });
        }

        [HttpPost]
        [Route("{key}")]
        public ActionResult Set(string key)
        {
            // NOTE: This is probably a terrible idea, but right now async endpoints are hitting
            // an assertion in CoreRT (GetRuntimeInterfaceMap() is not supported on this runtime.)
            // So we defer the actual async handling to later
            return new DeferredExecutionResult(async actionContext =>
            {
                var httpContext = actionContext.HttpContext;

                using var doc = new Data(new Key(key),
                    await Utf8JsonBody.ReadToEndAsync(httpContext.Request.Body, httpContext.RequestAborted));
                using var write = _store.BeginWrite();

                write.Set(doc);
            });
        }

        private void AllowSynchronousIO()
        {
            var syncIoFeature = HttpContext.Features.Get<IHttpBodyControlFeature>();
            if (syncIoFeature != null) syncIoFeature.AllowSynchronousIO = true;
        }
    }
}