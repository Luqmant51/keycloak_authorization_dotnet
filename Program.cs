using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

// JWT Authentication configuration
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Authority = "https://identity.teamfullstack.io/realms/luqman";
        options.Audience = "account";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyResolver = (token, securityToken, kid, validationParameters) =>
            {
                var client = new HttpClient();
                var jwks = client
                    .GetStringAsync("https://identity.teamfullstack.io/realms/luqman/protocol/openid-connect/certs")
                    .Result;
                var keys = new JsonWebKeySet(jwks).Keys;
                return keys;
            }
        };
        options.Events = new JwtBearerEvents()
        {
            OnTokenValidated = (context) =>
            {
                var realmAccess = context.Principal?.Claims
                    .FirstOrDefault(c => c.Type == "realm_access")?.Value;
                var accountRoles = context.Principal?.Claims
                    .FirstOrDefault(c => c.Type == "resource_access")?.Value;

                var isAdmin = realmAccess != null && realmAccess.Contains("admin");
                var canManageAccount = accountRoles != null && accountRoles.Contains("manage-account");

                Console.WriteLine("isAdmin");
                Console.WriteLine(isAdmin);
                Console.WriteLine("canManageAccount");
                Console.WriteLine(canManageAccount);
                return Task.CompletedTask;
            }
        };
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Please enter JWT with Bearer into field. Example: Bearer {your token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
