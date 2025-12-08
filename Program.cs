using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.StaticFiles;
using ARISESLCOM.Data;
using ARISESLCOM.Models.Domains;
using ARISESLCOM.Models.Domains.interfaces;
using ARISESLCOM.Models.Mappers;
using ARISESLCOM.Models.Mappers.interfaces;
using ARISESLCOM.Services;
using ARISESLCOM.Services.interfaces;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ARISESLCOM.Helpers;
using ARISESLCOM.Infrastructure.Config;
using Microsoft.Extensions.Hosting.WindowsServices;
using Amazon.DynamoDBv2;
using Amazon;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var redisConfiguration = builder.Configuration.GetSection("Redis")["ConnectionString"];

var jwtSecret = Environment.GetEnvironmentVariable("XPJ");
if (string.IsNullOrEmpty(jwtSecret))
    throw new Exception("JWT_SECRET nao configurado");

var jwtKey = Encoding.ASCII.GetBytes(jwtSecret);


//Redis Area
//var redis = ConnectionMultiplexer.Connect(redisConfiguration);/
//builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
//builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddSingleton<IRedisCacheService, MemoryCacheService>();
builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddControllersWithViews();

//Models DI
builder.Services.AddScoped<IGerenciaDomainModel, GerenciaDomainModel>();
builder.Services.AddScoped<IOrderDomainModel, OrderDomainModel>();
builder.Services.AddScoped<ICustomerDomainModel, CustomerDomainModel>();
builder.Services.AddScoped<IFreteDomainModel, FreteDomainModel>();
builder.Services.AddScoped<ICreditDomainModel, CreditDomainModel>();
builder.Services.AddScoped<IReportDomainModel, ReportDomainModel>();
builder.Services.AddScoped<IGroupDomainModel, GroupDomainModel>();
builder.Services.AddScoped<IProductDomainModel, ProductDomainModel>();
builder.Services.AddScoped<IShipmentDomainModel, ShipmentDomainModel>();
builder.Services.AddScoped<IDestaqueDomainModel, DestaqueDomainModel>();
builder.Services.AddScoped<IAcrDomainModel, AcrDomainModel>();
builder.Services.AddScoped<ILionServices, LionServices>();

//Mappers DI
builder.Services.AddScoped<IOrderViewMapper, OrderViewMapper>();
builder.Services.AddScoped<ICustomerViewMapper, CustomerViewMapper>();
builder.Services.AddScoped<IAirportViewMapper, AirportViewMapper>();
builder.Services.AddScoped<IProductViewMapper, ProductViewMapper>();
builder.Services.AddScoped<IGroupViewMapper, GroupViewMapper>();

//Others DI
builder.Services.AddScoped<IDBContext, ApplicationDbContext>();
builder.Services.AddScoped<ICorreiosService, CorreiosService>();
builder.Services.AddScoped<ISiteApiServices, SiteApiServices>();
builder.Services.AddScoped<IRedeService, RedeService>();

// DynamoDB Configuration
builder.Services.AddSingleton<IAmazonDynamoDB>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var region = configuration["AWS:TrackerPedidos:Region"];
    var accessKey = configuration["AWS:TrackerPedidos:AccessKey"];
    var secretKey = configuration["AWS:TrackerPedidos:SecretKey"];

    var dynamoConfig = new AmazonDynamoDBConfig();
    if (!string.IsNullOrWhiteSpace(region))
    {
        dynamoConfig.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
    }

    if (!string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey))
    {
        return new AmazonDynamoDBClient(accessKey, secretKey, dynamoConfig);
    }

    // Se não houver credenciais, usa credenciais padrão do ambiente (IAM Role, etc.)
    return new AmazonDynamoDBClient(dynamoConfig);
});

builder.Services.AddScoped<IDynamoDBService, DynamoDBService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});


builder.Services.AddHttpClient<ICorreiosService, CorreiosService>();
builder.Services.AddHttpClient<IRedeService, RedeService>();
builder.Services.AddHttpClient(Consts.SITE_CACHE_API, client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.Configure<SiteApiConfig>(
    builder.Configuration.GetSection("SiteApi"));

builder.Services.Configure<RedeConfig>(
    builder.Configuration.GetSection("Rede"));


builder.WebHost.UseKestrel(options =>
{
    options.ListenAnyIP(8058); // HTTP
});

var app = builder.Build();

//static files
var provider = new FileExtensionContentTypeProvider();
provider.Mappings.Add(string.Empty, "text/plain");
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    ServeUnknownFileTypes = true
});
//

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
