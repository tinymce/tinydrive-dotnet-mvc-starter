using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TinyDriveDotnetStarter.Models;
using TinyDriveDotNetStarter;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using System.Security.Cryptography.X509Certificates;

namespace TinyDriveDotnetStarter.Controllers
{
  public class HomeController : Controller
  {
    private string _apiKey { get; set; }
    private string _privateKeyFile { get; set; }

    private bool _scopeUser { get; set; }

    public HomeController(IConfiguration config)
    {
      var opts = config.GetSection("TinyDrive");
      _apiKey = opts["apiKey"];
      _privateKeyFile = opts["privateKeyFile"];
      _scopeUser = Boolean.Parse(opts["scopeUser"]);
    }

    public IActionResult Index()
    {
      return View(new LoginViewModel { Error = "" });
    }

    [HttpPost]
    public ActionResult Index(string login, string password)
    {
      HttpContext.Session.SetString("login", login);
      return RedirectToAction("Editor");
    }

    public IActionResult Editor()
    {
      var userName = HttpContext.Session.GetString("login");
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

      if (login != null)
      {
        var token = JwtHelper.CreateTinyDriveToken(login, "John Doe", _scopeUser, _privateKeyFile);
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
