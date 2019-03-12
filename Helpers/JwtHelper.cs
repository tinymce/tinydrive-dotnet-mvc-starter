using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using OpenSSL.PrivateKeyDecoder;

namespace TinyDriveDotnetStarter
{
  public static class JwtHelper
  {
    public static string CreateTinyDriveToken(string username, string fullname, bool scopeUser, string privateKeyFile)
    {
      var issued = ToUnixTime(DateTime.Now);
      var expires = ToUnixTime(DateTime.Now.AddMinutes(10));
      var privateKey = File.ReadAllText(privateKeyFile);

      var payload = new Dictionary<string, object> {
        { "sub", username },     // Unique user id string
        { "name", fullname },    // Full name of user
        { "iat", issued },
        { "exp", expires }
      };

      // When this is set the user will only be able to manage and see files in the specified root
      // directory. This makes it possible to have a dedicated home directory for each user.
      if (scopeUser) {
        payload["https://claims.tiny.cloud/drive/root"] = $"/{username}";
      }

      try {
        return CreateToken(payload, privateKey);
      } catch (FormatException) {
        throw new Exception($"Invalid RSA key in file: {privateKeyFile}");
      }
    }

    public static string CreateToken(object payload, string privateKey)
    {
      IOpenSSLPrivateKeyDecoder decoder = new OpenSSLPrivateKeyDecoder();
      using (RSACryptoServiceProvider cryptoServiceProvider = decoder.Decode(privateKey))
      {
        return Jose.JWT.Encode(payload, cryptoServiceProvider, Jose.JwsAlgorithm.RS256);
      }
    }

    private static long ToUnixTime(DateTime date)
    {
      var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      return Convert.ToInt64((TimeZoneInfo.ConvertTimeToUtc(date, TimeZoneInfo.Local) - epoch).TotalSeconds);
    }
  }
}