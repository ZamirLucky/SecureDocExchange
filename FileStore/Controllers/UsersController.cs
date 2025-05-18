using FileStore.Services;
using FileStore.ViewModels;
using Microsoft.AspNetCore.Authorization;
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

        [HttpGet, AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken, AllowAnonymous]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            if (!ModelState.IsValid)
                return View(registerViewModel);


            var result = await _userService.RegisterUser(
                registerViewModel.Email!, registerViewModel.FirstName!, registerViewModel.LastName!, registerViewModel.Password!);

            var all = _userService.GetAllUsers().ToList();
            Console.WriteLine("Currently in store: " + string.Join(", ", all.Select(u => u.Email)));

            if (result)
            {
                TempData["Message"] = "User registered successfully.";
                await Task.Delay(TimeSpan.FromSeconds(3));
                return RedirectToAction("Register");
            }

            ModelState.AddModelError(string.Empty, "Email already in use.");
            return View(registerViewModel);

        }


    }
}

