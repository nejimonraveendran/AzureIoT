using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;

        }

        [HttpGet]
        public IActionResult Index()
        {
            setUiStates();

            return View();
        }

        
        [HttpPost]
        public IActionResult Index(string currentState)
        {

            if(currentState== "on") 
            {
                settApplianceState("off");
            }
            else
            {
                settApplianceState("on");
            }

            setUiStates();
            return View();
        }




        private void setUiStates()
        {
            string buttonClass = string.Empty;
            string buttonText = string.Empty;
            string logoPath = string.Empty;

            string applianceState = getApplianceState();

            switch (applianceState)
            {
                case "on":
                    buttonClass = "btn-on";
                    logoPath = "/img/bulb-on.png";
                    buttonText = "TURN OFF";
                    break;
                case "off":
                    buttonClass = "btn-off";
                    logoPath = "/img/bulb-off.png";
                    buttonText = "TURN ON";
                    break;
                default:
                    buttonClass = "btn-waiting";
                    logoPath = "/img/bulb-off.png";
                    buttonText = "Can't reach Device, try refreshing!";
                    break;
            }

            @ViewBag.LogoPath = logoPath;
            @ViewBag.ButtonClass = buttonClass;
            ViewBag.ButtonText = buttonText;
            @ViewBag.CurrentState = applianceState;
        }



        private static string _state = "off";
        private string getApplianceState()
        {
            return _state;
        }

        private string settApplianceState(string state)
        {
            return _state = state;
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}