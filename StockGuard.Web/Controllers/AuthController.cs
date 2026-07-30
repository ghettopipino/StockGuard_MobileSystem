using Microsoft.AspNetCore.Mvc;
using StockGuard.Web.Services;
using StockGuard.Web.Models;

namespace StockGuard.Web.Controllers
{
    public class AuthController : Controller
    {
        private readonly FirebaseService _firebase;

        public AuthController(FirebaseService firebase)
        {
            _firebase = firebase;
        }

        // ── GET Login ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            // Redirect if already logged in
            if (HttpContext.Session
                .GetString("UserEmail") != null)
                return RedirectToAction(
                    "Index", "Dashboard");

            return View();
        }

        // ── POST Login ────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _firebase.GetUserByEmailAsync(
                model.Email.Trim().ToLower());

            if (user == null || user.Password != model.Password)
            {
                ViewBag.Error = "Incorrect email or password.";
                return View(model);
            }

            if (user.AccountStatus == "Pending")
            {
                ViewBag.Error = "Your account is pending approval.";
                return View(model);
            }

            if (user.AccountStatus == "Rejected")
            {
                ViewBag.Error = "Your account has been rejected.";
                return View(model);
            }

            if (!user.IsProjectEngineer)
            {
                ViewBag.Error = "Only Project Engineers can access the web system.";
                return View(model);
            }

            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserKey", user.UniqueKey);
            HttpContext.Session.SetString("UserRole", user.Role);

            return RedirectToAction("Index", "Dashboard");
        }

        // ── Logout ────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}