using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;

namespace Joonasw.ManagedIdentityDemos.CustomApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private static readonly IReadOnlyCollection<string> ClaimsToSendBack = new HashSet<string>
        {
            "iat", "nbf", "exp", "appid", "appidacr", "e_exp",
            "http://schemas.microsoft.com/identity/claims/objectidentifier",
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
        };

        [HttpGet]
        public ActionResult<Dictionary<string, string>> Get()
        {
            return User.Claims.Where(c => ClaimsToSendBack.Contains(c.Type)).ToDictionary(c => c.Type, c => c.Value);
        }
    }
}
