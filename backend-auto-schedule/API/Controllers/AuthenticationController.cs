using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginRequest
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public string Token { get; set; }
    }
    public class AuthenticationController : ControllerBase
    {
        private LoginResponse response = new LoginResponse { Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"};
        private string Login = "admin";
        private string Password = "password";

        [HttpPost]
        public IActionResult Post([FromBody] LoginRequest request)
        {
            if (request.Login == Login && request.Password == Password)
            {
                return Ok(response);
            }
            return Unauthorized();
        }
    }
}
