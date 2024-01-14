using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static CohesionNETCore.Services.CohesionService;
using System.Security.Claims;
using CohesionNETCore.Models;
using Microsoft.Extensions.Options;

namespace CohesionNETCore.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISession _session;
        private readonly ICohesionService _cohesionService;
        private IOptions<AppSettings> _appSettings;

        public AccountController(
            ILogger<HomeController> logger,
            IHttpContextAccessor httpContextAccessor,
            ICohesionService cohesionService,
            IOptions<AppSettings> appSettings)
        {
            _logger = logger;
            _session = httpContextAccessor.HttpContext.Session;
            _cohesionService = cohesionService;
            _appSettings = appSettings;
        }

        public IActionResult Login(string returnUrl = null)
        {
            try
            {
                string protocol = HttpContext.Request.Scheme;
                string hostname = HttpContext.Request.Host.Value;
                var response =
                    _cohesionService
                    .RequestAuth($"{protocol}://{hostname}/Account/Check", returnUrl);

                if (response == null)
                {
                    return BadRequest(new { message = "Errore nel processo di autenticazione" });
                }
                else
                {
                    return Redirect(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<IActionResult> Check([FromForm] string auth)
        {
            try
            {
                if (auth != null)
                {
                    var authResp = _cohesionService.CheckAuth(auth);

                    if (!authResp.error)
                    {
                        string errore = await LoginUtente(authResp.user);

                        if (errore != null)
                        {
                            _session.SetString("AlertErrore", $"{errore}");
                        }
                        else
                        {
                            // imposto le varibili cohesion in sessione
                            _session.SetString("idsessioneSSO", $"{authResp.idsessioneSSO}");
                            _session.SetString("idsessioneSSOASPNET", $"{authResp.idsessioneSSOASPNET}");
                        }
                    }
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception exc)
            {
                _session.SetString("AlertErrore", $"{exc.Message} - {exc.StackTrace}");
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // prendo l'url per effettuare la chiamata di logout a cohesion
                var ssoWebCheckSession = _appSettings.Value.SSOwebCheckSession;

                // prendo le variabili dalla sessione
                string idsessioneSSO = _session.GetString("idsessioneSSO");
                string idsessioneSSOASPNET = _session.GetString("idsessioneSSOASPNET");

                // effettuo la chiamata
                string token = _cohesionService.webCheckSessionSSO(
                    ssoWebCheckSession,
                    "LogoutSito",
                    idsessioneSSO,
                    idsessioneSSOASPNET);

                // effettuo il logout
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _logger.LogInformation("User logged out.");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return RedirectToAction("Index", "Home");
            }
        }

        private async Task<string> LoginUtente(string cf)
        {
            try
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, cf)
                };

                var claimsIdentity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme
                );

                var authProperties = new AuthenticationProperties
                {
                    //AllowRefresh = <bool>,
                    // Refreshing the authentication session should be allowed.

                    //ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
                    // The time at which the authentication ticket expires. A 
                    // value set here overrides the ExpireTimeSpan option of 
                    // CookieAuthenticationOptions set with AddCookie.

                    //IsPersistent = true,
                    // Whether the authentication session is persisted across 
                    // multiple requests. When used with cookies, controls
                    // whether the cookie's lifetime is absolute (matching the
                    // lifetime of the authentication ticket) or session-based.

                    //IssuedUtc = <DateTimeOffset>,
                    // The time at which the authentication ticket was issued.

                    //RedirectUri = <string>
                    // The full path or absolute URI to be used as an http 
                    // redirect response value.
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                // successo
                _logger.LogInformation("User logged in.");
                return null;
            }
            catch (Exception exc)
            {
                // errore
                return exc.Message;
            }
        }
    }
}
