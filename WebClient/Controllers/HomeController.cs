using Microsoft.AspNetCore.Mvc;
namespace WebClient.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => Redirect("~/index.html");
    }
}