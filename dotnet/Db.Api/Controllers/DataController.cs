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
        private readonly Lazy<DataReader> _reader;
        private readonly Lazy<DataWriter> _writer;

        public DataController(Lazy<DataReader> reader, Lazy<DataWriter> writer)
        {
            _reader = reader;
            _writer = writer;
        }

        [HttpGet]
        public JsonResult Get()
        {
            var values = _reader.Value.Data().ToList();
            
            return new JsonResult(values);
        }

        [HttpPost]
        [Route("{key}")]
        public ActionResult Set(string key, [FromBody] object value)
        {
            _writer.Value.Set(key, value);

            return Ok();
        }
    }
}