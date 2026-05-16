using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
