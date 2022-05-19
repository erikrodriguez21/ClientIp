using ClientIp.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace ClientIp.Controllers
{
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            ViewBag.clientIp = $"Client IP: {GetClientIPAddressWhitoutPort()} ";
            ViewBag.ExternalIP = $"App Service IP:  {await GetExternalIp("https://api.ipify.org/")}";
            ViewBag.SSL = $"Certificado: {await ValidateServerCertificate()}";
            

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }


        #region Methods
        public string GetClientIPAddress()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(',');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        public string GetClientIPAddressWhitoutPort()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            ServerVariables model = GetVariablesToModel(context);
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All,
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            };
            string json = JsonConvert.SerializeObject(model, jsonSerializerSettings)
                .Replace("\"$type\": \"ClientIp.Models.ServerVariables, ClientIp\",", "")
                .Replace("\\r\\n","")
                .Trim();

            ViewBag.ServerInfo = $"Server Info: \n { json }";

            if (!string.IsNullOrEmpty(ipAddress))
            {
                string[] addresses = ipAddress.Split(':');
                if (addresses.Length != 0)
                {
                    return addresses[0];
                }
            }

            string[] res = context.Request.ServerVariables["REMOTE_ADDR"].Split(':');

            return res[0];
        }

        public ServerVariables GetVariablesToModel(System.Web.HttpContext context)
        {
            ServerVariables obj = Activator.CreateInstance<ServerVariables>();

            int i = 0;
            while (i < context.Request.ServerVariables.AllKeys.Count())
            {
                foreach (var item in context.Request.ServerVariables.AllKeys)
                {
                    foreach (PropertyInfo prop in obj.GetType().GetProperties())
                    {
                        if (prop.Name == item)
                        {
                            prop.SetValue(obj, context.Request.ServerVariables[item]);
                        }
                    }
                }
                i++;
            }

            return obj;
        }


        public async Task<string> GetExternalIp(string MethodWithParameters, string accessToken = null)
        {
            string response = "";
           
            using (var client = new HttpClient())
            {
                // CertificateValidation
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = (message, cert, chain, sslPolicyErrors) => {
                   
                    return true;
                };

                client.Timeout = TimeSpan.FromMinutes(10);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                if (!string.IsNullOrEmpty(accessToken))
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                var result = await client.GetAsync(MethodWithParameters);
                var resultContent = await result.Content.ReadAsStringAsync();
                response = resultContent;
                if (result.IsSuccessStatusCode)
                {
                    string[] ip = resultContent.Split(':');
                    return resultContent == "::1" ? "127.0.0.1" : ip[0];
                }
            }
            return response;
        }

        public async Task<string> ValidateServerCertificate()
        {
            string json = "";
            using (var client = new HttpClient())
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = (message, cert, chain, sslPolicyErrors) => {
                    json = JsonConvert.SerializeObject(cert.Issuer);
                    return true;
                };
                var result = await client.GetAsync("https://api.ipify.org/");
                return json;
            }  

        }

        #endregion






    }
}