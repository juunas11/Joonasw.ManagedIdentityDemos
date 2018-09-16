using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Joonasw.ManagedIdentityDemos.CustomApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<Dictionary<string, string>> Get()
        {
            return User.Claims.ToDictionary(c => c.Type, c => c.Value);
        }
    }
}
