using API.Services;
using API.Settings;

var builder = WebApplication.CreateBuilder(args);

// Register Redis
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddScoped<RedisService>();

// Register CardProviderService
builder.Services.AddScoped<CardProviderService>();

// Register Controllers
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add this if you want to specify the port
builder.WebHost.UseUrls("http://localhost:5000");

// Register HttpClient and ApiService
builder.Services.AddHttpClient();
builder.Services.AddScoped<ApiService>();

// Register services
builder.Services.AddScoped<ICreditCardService, CreditCardService>();
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddScoped<RedisService>();

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
