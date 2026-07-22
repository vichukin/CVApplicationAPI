using CVApplicationAPI.Services;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://cvapplicationclientside.vercel.app") // Разрешаем твой фронтенд
              .AllowAnyHeader()                     // Разрешаем любые заголовки (нужно для Content-Type: application/json)
              .AllowAnyMethod();                    // Разрешаем любые методы (POST, GET, OPTIONS и т.д.)
    });
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register HttpClientFactory for HTTP operations (used by CvCacheService and CvRefreshWorker)
builder.Services.AddHttpClient();

// Register CV Cache Service as a singleton for lazy-loaded in-memory caching
builder.Services.AddSingleton<CvCacheService>();

// Register the background refresh worker as a hosted service
builder.Services.AddHostedService<CVApplicationAPI.Services.CvRefreshWorker>();

// Bind OpenAI settings from configuration (section: "OpenAI"). Optional.
builder.Services.Configure<OpenAISettings>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OpenAISettings>>().Value);

// Register Semantic Kernel as a singleton. Configure OpenAI Chat Completion using settings.
builder.Services.AddSingleton<Kernel>(sp =>
{
    var settings = sp.GetRequiredService<OpenAISettings>();

    var builderKernel = Kernel.CreateBuilder();

    if (!string.IsNullOrWhiteSpace(settings?.ApiKey) && !string.IsNullOrWhiteSpace(settings?.ModelId))
    {
        // Extension method provided by Microsoft.SemanticKernel.Connectors.OpenAI
        builderKernel = builderKernel.AddOpenAIChatCompletion(settings.ModelId, settings.ApiKey);
    }

    var kernel = builderKernel.Build();
    return kernel;
});

// Register application services
builder.Services.AddScoped<CVApplicationAPI.Services.IChatService, CVApplicationAPI.Services.ChatService>();

//Console.WriteLine($"[DEBUG-CONFIG] Links:AIContext = '{builder.Configuration["Links:AIContext"]}'");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowLocalhost");
app.UseAuthorization();

app.MapControllers();

app.Run();
