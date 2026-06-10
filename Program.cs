using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

using AspNetMvcApp.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Only include API controllers, exclude MVC controllers
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return apiDesc.RelativePath != null && apiDesc.RelativePath.StartsWith("api/");
    });
});

// Register DbContext with SQL Server Connection String
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Chatbot Service
builder.Services.AddHttpClient();
builder.Services.AddScoped<AspNetMvcApp.Services.IChatbotService, AspNetMvcApp.Services.ChatbotService>();

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Password settings (relaxed for development)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure Google Authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.CallbackPath = "/signin-google";
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect("/Account/Login?externalError=Google");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

var app = builder.Build();

// Seed Roles and Default Users
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated();

        var hasFullNameColumn = await context.Database
            .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id = OBJECT_ID(N'AspNetUsers') AND name = N'FullName'")
            .SingleAsync();

        if (hasFullNameColumn == 0)
        {
            await context.Database.ExecuteSqlRawAsync("ALTER TABLE AspNetUsers ADD FullName NVARCHAR(MAX) NULL;");
        }

        await EnsureOrderTablesAsync(context);

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<AppUser>>();

        // Create Roles
        string[] roles = { "Admin", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin user
        var adminEmail = "admin@huy.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Administrator",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed regular User
        var userEmail = "user@huy.com";
        if (await userManager.FindByEmailAsync(userEmail) == null)
        {
            var regularUser = new AppUser
            {
                UserName = userEmail,
                Email = userEmail,
                FullName = "Huy Nguyen",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(regularUser, "User@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(regularUser, "User");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra khi tạo hoặc khởi tạo dữ liệu cho Database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
else
{
    app.UseStaticFiles(); // Serve CSS for Swagger UI custom theme
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cart API v1");
        c.InjectStylesheet("/css/swagger-custom.css");
    });
}
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Area routing (must be before default route)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllers();


app.Run();

static async Task EnsureOrderTablesAsync(AppDbContext context)
{
    await context.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'[Orders]', N'U') IS NULL
        BEGIN
            CREATE TABLE [Orders] (
                [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_Orders] PRIMARY KEY,
                [OrderNumber] NVARCHAR(32) NOT NULL,
                [UserId] NVARCHAR(450) NULL,
                [CustomerName] NVARCHAR(200) NOT NULL,
                [Phone] NVARCHAR(50) NOT NULL,
                [Address] NVARCHAR(500) NOT NULL,
                [PaymentMethod] NVARCHAR(100) NOT NULL,
                [Notes] NVARCHAR(MAX) NOT NULL,
                [TotalAmount] DECIMAL(18,2) NOT NULL,
                [Status] NVARCHAR(50) NOT NULL,
                [CreatedAt] DATETIME2 NOT NULL,
                [UpdatedAt] DATETIME2 NOT NULL
            );

            CREATE UNIQUE INDEX [IX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);
            CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
            CREATE INDEX [IX_Orders_CreatedAt] ON [Orders] ([CreatedAt]);
        END
        """);

    await context.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'[OrderItems]', N'U') IS NULL
        BEGIN
            CREATE TABLE [OrderItems] (
                [Id] INT IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OrderItems] PRIMARY KEY,
                [OrderId] INT NOT NULL,
                [ProductId] INT NULL,
                [ProductName] NVARCHAR(300) NOT NULL,
                [UnitPrice] DECIMAL(18,2) NOT NULL,
                [Quantity] INT NOT NULL,
                [ImagePath] NVARCHAR(500) NOT NULL,
                CONSTRAINT [FK_OrderItems_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE CASCADE,
                CONSTRAINT [FK_OrderItems_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE SET NULL
            );

            CREATE INDEX [IX_OrderItems_OrderId] ON [OrderItems] ([OrderId]);
            CREATE INDEX [IX_OrderItems_ProductId] ON [OrderItems] ([ProductId]);
        END
        """);
}
