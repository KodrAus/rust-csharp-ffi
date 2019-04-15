using System;
using System.Linq;
using Db.Api.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Db.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private readonly Lazy<DataStore> _store;

        public DataController(Lazy<DataStore> store)
        {
            _store = store;
        }

        [HttpGet]
        public JsonResult Get()
        {
            using (var reader = _store.Value.BeginRead())
            {
                var values = reader.Data().ToList();

                return new JsonResult(values);
            }
            
        }

        [HttpPost]
        [Route("{key}")]
        public ActionResult Set(string key, [FromBody] object value)
        {
            using (var writer = _store.Value.BeginWrite())
            {
                writer.Set(new Data(key, value));

                return Ok();
            }
        }

        [HttpDelete]
        [Route("{key}")]
        public ActionResult Remove(string key)
        {
            using (var deleter = _store.Value.BeginDelete())
            {
                deleter.Remove(key);

                return Ok();
            }
        }
    }
}