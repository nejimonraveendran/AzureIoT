using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplianceService _applianceService;
        private readonly UserService _userService;

        public HomeController(ILogger<HomeController> logger, ApplianceService applianceService, UserService userService)
        {
            _logger = logger;
            _applianceService = applianceService;
            _userService = userService;

        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }


        [HttpGet("/appliance/status")]
        public async Task<ApplianceStatus> GetApplianceStatus()
        {
            try
            {
                return await _applianceService.GetApplianceStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling GetApplianceStateAsync");

                return ApplianceStatus.Unknown;
            }

        }


        [HttpPost("/appliance/status/toggle")]
        public async Task<ApplianceStatus> ToggleApplianceStatus()
        {
            try
            {
                return await _applianceService.ToggleApplianceStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling SetApplianceStateAsync");

                return ApplianceStatus.Unknown;
            }
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                var user = _userService.FindUserByUserNameAndPassword(model.UserName, model.Password);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View();
                }

                var ci = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                ci.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.UserName));
                ci.AddClaim(new Claim(ClaimTypes.Name, user.Name));
                
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(ci), 
                    new AuthenticationProperties 
                    { 
                        IsPersistent = true,
                        IssuedUtc= DateTime.UtcNow,
                        ExpiresUtc= DateTime.UtcNow.AddYears(1), //intentionally set to 1 year to avoid logging in too often.
                        
                    });

                return RedirectToAction("Index");

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calling FindUserByUserNameAndPassword");

                ModelState.AddModelError(string.Empty, "Unexpected error!");
                return View();
            }

            
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index");
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult Hash()
        {
            return View();
        }


        [AllowAnonymous]
        [HttpPost]
        public IActionResult Hash(HashViewModel hashViewModel)
        {
            var hash = _userService.GetHash(hashViewModel.Password, hashViewModel.Salt);

            ViewBag.Hash = hash;    

            return View(hashViewModel);
        }

    }
}