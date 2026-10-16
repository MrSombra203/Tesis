using System;
using System.Text;
using System.Text.RegularExpressions;

namespace GestionInteligente.Auth
{
    public static class JwtPayloadReader
    {
        public static string ObtenerClaim(string jwt, string nombreClaim)
        {
            if (string.IsNullOrEmpty(jwt)) return null;

            var partes = jwt.Split('.');
            if (partes.Length < 2) return null;

            var payloadJson = DecodificarBase64Url(partes[1]);

            var match = Regex.Match(payloadJson, $"\"{nombreClaim}\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string DecodificarBase64Url(string base64Url)
        {
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
        }
    }
}
