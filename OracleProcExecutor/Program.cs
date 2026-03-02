using OracleProcExecutor.Services;

var builder = WebApplication.CreateBuilder(args);

// IIS Integration
builder.WebHost.UseIISIntegration();

// Services
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Keep property names as-is (PascalCase) to match Oracle param names
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.WriteIndented = true;
    });

builder.Services.AddScoped<IParameterDiscoveryService, ParameterDiscoveryService>();
builder.Services.AddScoped<IOracleExecutorService, OracleExecutorService>();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title       = "One Service 2 Rule Them All!",
        Version     = "v1",
        Description = "One query to find them, one query to bring them all, " +
                      "and in the darkness bind them. " +
                      "Executes any Oracle stored procedure or function dynamically — " +
                      "parameter types are auto-discovered from ALL_ARGUMENTS."
    });

    // Include XML comments if available
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// Static files — needed to serve wwwroot/swagger-ui/lotr.css
app.UseStaticFiles();

// Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "One Service 2 Rule Them All! v1");
    c.RoutePrefix    = "swagger";
    c.DocumentTitle  = "One Service 2 Rule Them All!";

    // Google Fonts (Cinzel) + favicon preconnect
    c.HeadContent = """
        <link rel="preconnect" href="https://fonts.googleapis.com">
        <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
        <link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@400;600;900&family=Cinzel+Decorative:wght@400;700;900&display=swap" rel="stylesheet">
        """;

    // Inject LOTR theme
    c.InjectStylesheet("/swagger-ui/lotr.css");

    // Expand tags by default so the Ring calls are visible immediately
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

app.UseAuthorization();
app.MapControllers();

app.Run();
