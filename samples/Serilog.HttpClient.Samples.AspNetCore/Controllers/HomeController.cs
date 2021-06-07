using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog.HttpClient.Samples.AspNetCore.Services;

namespace Serilog.HttpClient.Samples.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMyService _myService;

        public HomeController(IMyService myService)
        {
            _myService = myService;
        }
        
        public async Task<IActionResult> Index()
        {
           var result =  await _myService.SendRequest();
           return Ok(result);
        }
    }
}