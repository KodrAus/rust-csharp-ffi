using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Db.Storage;

namespace Db.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        public ValuesController(Store store)
        {
            _store = store;
        }

        readonly Store _store;

        [HttpGet]
        public bool Get()
        {
            return _store.IsOpen;
        }
    }
}
