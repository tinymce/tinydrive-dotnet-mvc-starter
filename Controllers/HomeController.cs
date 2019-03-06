using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using TinyDriveDotnetStarter.Models;
using System.Linq;

namespace TinyDriveDotnetStarter.Controllers
{
  class User
  {
    public string Login { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
  }

  public class HomeController : Controller
  {
    private string _apiKey { get; set; }
    private string _privateKeyFile { get; set; }
    private bool _scopeUser { get; set; }
    private User[] _users { get; set; }

    public HomeController(IConfiguration config)
    {
      var opts = config.GetSection("TinyDrive");
      _apiKey = opts["apiKey"];
      _privateKeyFile = opts["privateKeyFile"];
      _scopeUser = Boolean.Parse(opts["scopeUser"]);
      _users = opts.GetSection("users")
        .GetChildren()
        .Select(user => new User { Login = user["login"], Password = user["password"], Name = user["name"] })
        .ToArray();
    }

    public IActionResult Index()
    {
      return View(new LoginViewModel { Error = "" });
    }

    [HttpPost]
    public ActionResult Index(string login, string password)
    {
      var user = _users.FirstOrDefault(u => u.Login == login && u.Password == password);

      if (user == null)
      {
        return View(new LoginViewModel { Error = "Invalid username/password." });
      }
      else
      {
        HttpContext.Session.SetString("login", user.Login);
        HttpContext.Session.SetString("name", user.Name);
        return RedirectToAction("Editor");
      }
    }

    public IActionResult Editor()
    {
      var userName = HttpContext.Session.GetString("name");
      if (userName == null)
      {
        return RedirectToAction("Index");
      }
      else
      {
        return View(new EditorViewModel { ApiKey = _apiKey, UserName = userName });
      }
    }

    [HttpPost]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public JsonResult Jwt()
    {
      var login = HttpContext.Session.GetString("login");
      var name = HttpContext.Session.GetString("name");

      if (login != null)
      {
        var token = JwtHelper.CreateTinyDriveToken(login, name, _scopeUser, _privateKeyFile);
        return Json(new { token });
      }
      else
      {
        var result = Json(new { error = "Failed to auth." });
        result.StatusCode = 403;
        return result;
      }
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Logout()
    {
      HttpContext.Session.Clear();
      return RedirectToAction("Index");
    }
  }
}
