using Maurer.OktaFilter;
using Maurer.OktaFilter.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTesting.Utilities
{
    [ApiController]
    [Route("Integrations/AuthenticationFilter")]
    [ServiceFilter(typeof(AuthenticationFilter<TokenService>))]
    public class IntegrationController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok();
    }
}
