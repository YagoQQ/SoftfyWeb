using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SoftfyWeb.Data;
using SoftfyWeb.Modelos;
using SoftfyWeb.Services;
using System.Text;
using Microsoft.Extensions.FileProviders;
using System.IO;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar Identity con roles, confirmación de cuenta y correo único
builder.Services.AddIdentity<Usuario, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true; // Evita registros con el mismo correo
    // Configuración de lockout
    options.Lockout.MaxFailedAccessAttempts = 5;  // Número máximo de intentos fallidos antes de bloquear la cuenta
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);  // Tiempo de bloqueo (1 minuto)
    options.Lockout.AllowedForNewUsers = true;  // Permite el lockout para usuarios nuevos
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configurar autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

// Registrar servicios
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<JwtService>(); // Registrar el servicio JwtService

// Configurar CORS para el front MVC
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWeb",
        policy => policy
            .WithOrigins("https://localhost:7130")  // Tu frontend MVC
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Servir archivos estáticos desde la carpeta ArchivosCanciones
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "ArchivosCanciones")),
    RequestPath = "/ArchivosCanciones"
});


app.UseHttpsRedirection();

// Usar CORS
app.UseCors("AllowWeb");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
