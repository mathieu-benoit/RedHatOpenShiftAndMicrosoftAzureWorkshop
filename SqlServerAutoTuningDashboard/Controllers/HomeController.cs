using Microsoft.AspNetCore.Mvc;
using System.Runtime.InteropServices;

namespace SqlServerAutoTuningDashboard.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            ViewData["OSDescription"] = RuntimeInformation.OSDescription;
            ViewData["ArchitectureImage"] = "sql2017rhel74-AutoTuning-Demo-2.png";
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}