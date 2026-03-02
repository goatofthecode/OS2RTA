using System.Web;
using System.Web.Http;
using OracleProcExecutor.App_Start;

namespace OracleProcExecutor
{
    public class WebApiApplication : HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            // SwaggerConfig is auto-registered via WebActivatorEx
        }
    }
}
