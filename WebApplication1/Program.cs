using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OllamaSharp;
using WebApplication1;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .SetIsOriginAllowed(origin => true);
});
});

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthorization();


builder.Services.AddIdentityApiEndpoints<IdentityUser>().AddEntityFrameworkStores<ApplicationDbContext>();


var configValue = builder.Configuration["ConnectionStrings:WebApiDatabase"];

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(configValue));

var app = builder.Build();
// set up the client

var uri = new Uri("http://localhost:11434");
var ollama = new OllamaApiClient(uri)
{
    // select a model which should be used for further operations
    SelectedModel = "phi3"
};

/*using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}*/

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGroup("/identity").MapIdentityApi<IdentityUser>();


app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})

    .RequireAuthorization()
.WithName("GetWeatherForecast")
.WithOpenApi();

app.MapPost("/ollama", async ([FromBody] MessageModel model) =>
{
    var entireMessage = "";
    var chat = ollama.Chat(stream =>
    {
        if (!stream.Done)
        {
            entireMessage += stream.Message?.Content ?? "";
        }
    });
    await chat.Send(model.messaggio);
    return entireMessage;
})

    .RequireAuthorization()
.WithName("GetOllama")
.WithOpenApi();





app.Run();


internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}



