
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zumlo.Gateway.Application;

namespace Zumlo.Gateway.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SessionsController : ControllerBase
    {
        private readonly SessionManager _sessions;
        public SessionsController(SessionManager sessions) => _sessions = sessions;

        [HttpGet("{id}/summary")]
        public IActionResult GetSummary(string id)
        {
            var summary = _sessions.GetSummary(id);
            return summary is null ? NotFound(new { error = new { code = "not_found", message = "Session not found" } }) : Ok(summary);
        }
    }
}
