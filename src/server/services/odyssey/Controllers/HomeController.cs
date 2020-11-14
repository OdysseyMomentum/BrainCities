//using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Odyssey.API.Controllers
{
    public partial class SensorsController
    {
        [Route("")]
        public class HomeController : Controller
        {
            [HttpGet()]
            public IActionResult Index()
            {
                return new RedirectResult("~/swagger");
            }
        }

    }
}