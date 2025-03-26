    using Microsoft.EntityFrameworkCore;
    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Web.Models;
    using Web.Reponsitory;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Authorization;

    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));




    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddDefaultTokenProviders()
        .AddDefaultUI()
        .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.AddHttpClient();
builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
        options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User"));
    });

    builder.Services.AddRazorPages();
    builder.Services.AddControllersWithViews();
    builder.Services.AddScoped<IProductRepository, EFProductRepository>();
    builder.Services.AddScoped<ICategoryRepository, EFCategoryRepository>();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseStatusCodePages(async context =>
    {
        if (context.HttpContext.Response.StatusCode == 403)
        {
            context.HttpContext.Response.Redirect("/Home/AccessDenied");
        }
    });
    app.MapRazorPages();
    app.Use(async (context, next) =>
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
        {
            // Nếu endpoint có [AllowAnonymous], bỏ qua kiểm tra đăng nhập và tiếp tục
            await next();
            return;
        }

        var user = context.User;
        var path = context.Request.Path;

        if (!user.Identity!.IsAuthenticated &&
            !path.StartsWithSegments("/Identity/Account/Login") &&
            !path.StartsWithSegments("/Identity/Account/Register"))
        {
            context.Response.Redirect("/Identity/Account/Login");
            return;
        }

        await next();
    });
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");
    // Thêm ngay trước app.Run()
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await RoleSeeder.SeedRoles(services);
    }

    app.Run();
