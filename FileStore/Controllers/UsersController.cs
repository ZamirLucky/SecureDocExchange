using FileStore.Services;
using FileStore.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FileStore.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            if (!ModelState.IsValid)
                return View(registerViewModel);


            var result = await _userService.RegisterUser(
                registerViewModel.Email, registerViewModel.Password, registerViewModel.FirstName, registerViewModel.LastName);

            if (result)
            {
                TempData["Message"] = "User registered successfully.";
                return RedirectToAction("Register");
            }

            ModelState.AddModelError("", "User registration failed.");
            return View(registerViewModel);

        }
    }
}

