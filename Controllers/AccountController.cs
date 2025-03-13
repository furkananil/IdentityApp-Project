using System.Reflection.Metadata.Ecma335;
using AspNetCoreGeneratedDocument;
using IdentityApp.Models;
using IdentityApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace IdentityApp.Controllers
{
    public class AccountController : Controller
    {
        private UserManager<AppUser> _userManager;
        private RoleManager<AppRole> _roleManager;
        private SignInManager<AppUser> _signInManager;
        public AccountController(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if(user != null)
                {
                    await _signInManager.SignOutAsync();

                    if(!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        ModelState.AddModelError("", "Email adresinizi onaylayin");
                        return View(model);
                    }

                    var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, true);

                    if(result.Succeeded)
                    {
                        await _userManager.ResetAccessFailedCountAsync(user);
                        await _userManager.SetLockoutEndDateAsync(user, null);

                        return RedirectToAction("Index", "Home");
                    }
                    else if(result.IsLockedOut)
                    {
                        var lockoutDate = await _userManager.GetLockoutEndDateAsync(user);
                        var timeLeft = lockoutDate.Value - DateTime.UtcNow;

                        ModelState.AddModelError("", $"Hesabiniz kitlendi lutfen {timeLeft.Minutes} dakika sonra tekrar deneyin");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Sifreniz hatali");
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Email adresi hatali");
                }
            }
            return View(model);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateViewModel model)
        {
            if(ModelState.IsValid)
            {
                var user = new AppUser  {
                    UserName = model.UserName,
                    Email = model.Email,
                    FullName = model.FullName
                };

                IdentityResult result = await _userManager.CreateAsync(user, model.Password);

                if(result.Succeeded)
                {
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var url = Url.Action("ConfirmEmail", "Account", new { user.Id, token});

                    //email
                    // await _emailSender.SendEmailAsync(user.Email, "Hesap Onayi", "Lutfen hesabini onaylamak icin linke tiklayiniz"())

                    TempData["message"] = "Email adresinizi onaylayın.";  
                    return RedirectToAction("Login", "Account");
                }

                foreach(IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string Id, string token)
        {
            if(Id == null || token == null)
            {
                TempData["message"] = "Geçersiz token";   
                return View();
            }

            var user = await _userManager.FindByIdAsync(Id);

            if(user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user,token);

                if(result.Succeeded)
                {
                    TempData["message"] = "Hesabiniz onaylandi";
                    return RedirectToAction("Login", "Account");
                }
            }
            TempData["message"] = "Hesabiniz onaylanmadi";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string Email)
        {
            if(string.IsNullOrEmpty(Email))
            {
                TempData["message"] = "eposta adresini ginriniz";
                return View();
            }

            var user = await _userManager.FindByEmailAsync(Email);

            if(user == null)
            {
                TempData["message"] = "eposyata eslesen bir kayit yok";
                return View();
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var url = Url.Action("ResetPassword", "Account", new { user.Id, token});

            //email
            // await _emailSender.SendEmail
            // Async(user.Email, "Hesap Onayi", "Lutfen hesabini onaylamak icin linke tiklayiniz"())

            TempData["message"] = "Epostaya gonderilen link ile sifrenizi sifirlayniz";

            return View();

        }

        public IActionResult ResetPassword(string Id, string token)
        {
            if(Id == null || token == null)
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordModel { Token = token};
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            if(ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if(user == null)
                {
                    TempData["message"] = "kayit yok";
                    return RedirectToAction("Login");
                }
                var result = await _userManager.ResetPasswordAsync(user,model.Token,model.Password);

                if(result.Succeeded)
                {
                    TempData["message"] = "sifre degistirildi";
                    return RedirectToAction("Login");
                }
                foreach(IdentityError error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }
    }
}