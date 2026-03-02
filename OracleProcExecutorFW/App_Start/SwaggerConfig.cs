using System.Web.Http;
using Swashbuckle.Application;

[assembly: WebActivatorEx.PreApplicationStartMethod(
    typeof(OracleProcExecutor.App_Start.SwaggerConfig), "Register")]

namespace OracleProcExecutor.App_Start
{
    public static class SwaggerConfig
    {
        public static void Register()
        {
            var thisAssembly = typeof(SwaggerConfig).Assembly;

            GlobalConfiguration.Configuration
                .EnableSwagger(c =>
                {
                    c.SingleApiVersion("v1", "One Service 2 Rule Them All!")
                     .Description(
                        "One query to find them, one query to bring them all, " +
                        "and in the darkness bind them. " +
                        "Executes any Oracle stored procedure or function — " +
                        "parameter types auto-discovered from ALL_ARGUMENTS.");
                })
                .EnableSwaggerUi("swagger/ui/{*assetPath}", c =>
                {
                    // Replace the default index with our custom LOTR-themed page.
                    // The HTML is embedded in the assembly as:
                    //   OracleProcExecutor.SwaggerUI.index.html
                    c.CustomAsset("index", thisAssembly,
                        "OracleProcExecutor.SwaggerUI.index.html");

                    c.DocumentTitle("One Service 2 Rule Them All!");

                    // Expand the operations list on load
                    c.DocExpansion(DocExpansion.List);
                });
        }
    }
}
