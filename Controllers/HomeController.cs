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
    public string UserName { get; set; }
    public string Password { get; set; }
    public string FullName { get; set; }
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
        .Select(user => new User { UserName = user["username"], Password = user["password"], FullName = user["fullname"] })
        .ToArray();
    }

    public IActionResult Index()
    {
      return View(new LoginViewModel { Error = "" });
    }

    [HttpPost]
    public ActionResult Index(string username, string password)
    {
      var user = _users.FirstOrDefault(u => u.UserName == username && u.Password == password);

      if (user == null)
      {
        return View(new LoginViewModel { Error = "Invalid username/password." });
      }
      else
      {
        HttpContext.Session.SetString("username", user.UserName);
        HttpContext.Session.SetString("fullname", user.FullName);
        return RedirectToAction("Editor");
      }
    }

    public IActionResult Editor()
    {
      var fullName = HttpContext.Session.GetString("fullname");
      if (fullName == null)
      {
        return RedirectToAction("Index");
      }
      else
      {
        return View(new EditorViewModel { ApiKey = _apiKey, FullName = fullName });
      }
    }

    [HttpPost]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public JsonResult Jwt()
    {
      var username = HttpContext.Session.GetString("username");
      var fullname = HttpContext.Session.GetString("fullname");

      if (username != null)
      {
        var token = JwtHelper.CreateTinyDriveToken(username, fullname, _scopeUser, _privateKeyFile);
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
