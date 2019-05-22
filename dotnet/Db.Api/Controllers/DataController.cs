using System.Text.Json;
using Db.Api.Mvc;
using Db.Api.Storage;
using Db.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Db.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : CoreRtControllerBase
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
            return Defer(async ctx =>
            {
                var body = ctx.Response.Body;
                
                using var reader = outerReader;
                using var writer = new Utf8JsonWriter(body);

                writer.WriteStartArray();
                foreach (var outerData in reader.Data())
                {
                    using var data = outerData;
                    data.WriteAsValue(writer);
                }

                writer.WriteEndArray();

                await writer.FlushAsync(ctx.RequestAborted);
            });
        }

        [HttpPost]
        [Route("{key}")]
        public ActionResult Set(string key)
        {
            return Defer(async ctx =>
            {
                using var doc = new Data(new Key(key),
                    await Utf8JsonBody.ReadToEndAsync(ctx.Request.Body, ctx.RequestAborted));
                using var write = _store.BeginWrite();

                write.Set(doc);
            });
        }

        [HttpDelete]
        [Route("{key}")]
        public ActionResult Remove(string key)
        {
            using var remove = _store.BeginDelete();
            
            remove.Remove(new Key(key));

            return Ok();
        }
    }
}
