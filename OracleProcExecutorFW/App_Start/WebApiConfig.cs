using System.Web.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OracleProcExecutor.App_Start
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Use Newtonsoft JSON with PascalCase (preserves Oracle param names)
            var settings = config.Formatters.JsonFormatter.SerializerSettings;
            settings.ContractResolver  = new DefaultContractResolver();
            settings.Formatting        = Formatting.Indented;
            settings.NullValueHandling = NullValueHandling.Include;

            // Remove XML formatter — JSON only
            config.Formatters.Remove(config.Formatters.XmlFormatter);

            // Attribute routing for [RoutePrefix] / [Route] on controllers
            config.MapHttpAttributeRoutes();

            // Fallback conventional route
            config.Routes.MapHttpRoute(
                name:         "DefaultApi",
                routeTemplate:"api/{controller}/{action}/{id}",
                defaults:     new { id = RouteParameter.Optional }
            );
        }
    }
}
