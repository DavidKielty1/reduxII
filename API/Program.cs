using API.Services;
using API.Services.Interfaces;
using API.Settings;

var builder = WebApplication.CreateBuilder(args);

// Register Redis
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddScoped<IRedisService, RedisService>();

// Register HttpClient
builder.Services.AddHttpClient();

// Register Services in correct order
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<ICardProviderService, CardProviderService>();
builder.Services.AddScoped<ICreditCardService, CreditCardService>();

// Register Controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add this if you want to specify the port
builder.WebHost.UseUrls("http://localhost:5000");

var app = builder.Build();

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();
