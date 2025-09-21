using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Waltrack.Models;


namespace Waltrack.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }



        // ---------------- REGISTER ----------------

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    CreatedAt = DateTime.UtcNow,
                };

                //Creates a new user in the database with a hashed password.
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    //Signs in the user immediately
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Dashboard");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }



        // ---------------- LOGIN ----------------
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                //Checks credentials and signs in the user if correct.
                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: true); // lockoutOnFailure: true → Locks the user account after multiple failed login attempts. 

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user != null)
                    {
                        user.LastLogin = DateTime.UtcNow;
                        await _userManager.UpdateAsync(user);
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    return RedirectToAction("Index", "Dashboard");
                }

                if (result.IsLockedOut)
                {
                    ModelState.AddModelError("", "Account locked. Try again later.");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid login attempt.");
                }
            }

            return View(model);
        }


        // ---------------- LOGOUT ----------------
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


        // ---------------- ACCESS DENIED ----------------
        public IActionResult AccessDenied()
        {
            return View();
        }
    }

}

