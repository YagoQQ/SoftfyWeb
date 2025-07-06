using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1) Servicios
builder.Services.AddControllersWithViews();

// 2) HttpClient nombrado
builder.Services.AddHttpClient("SoftfyApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]);  // p.ej. "https://localhost:7003/api/"
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// 3) Autenticación con Cookie (almacena el JWT ahí)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/VistasAuth/Login";
        options.LogoutPath = "/VistasAuth/Logout";
        options.Cookie.Name = "auth_cookie";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
    });

var app = builder.Build();

// 4) Middleware de excepciones y HSTS
if (app.Environment.IsDevelopment())
{
    //In Dev, seethefull stack on screen
    app.UseDeveloperExceptionPage();
}
else
{
    // En Prod, redirige a tu acción Error
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 5) Resto del pipeline
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 6) Autenticación / Autorización
app.UseAuthentication();
app.UseAuthorization();

// 7) Mapear rutas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
