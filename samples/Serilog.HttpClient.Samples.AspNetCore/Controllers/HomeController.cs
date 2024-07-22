using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog.HttpClient.Samples.AspNetCore.Services;

namespace Serilog.HttpClient.Samples.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMyService _myService;

        // for demonstrating multiple configurations of HttpClient
        private readonly IMyOtherService _myOtherService;

        public HomeController(IMyService myService, IMyOtherService myOtherService)
        {
          _myService = myService;
          _myOtherService = myOtherService;
        }
        
        public async Task<IActionResult> Index()
        {
           var result =  await _myService.SendRequest();
           return Ok(result);
        }

        public async Task<IActionResult> Other()
        {
          var otherResult = await _myOtherService.SendRequest();
          return Ok(otherResult);
        }
    }
}