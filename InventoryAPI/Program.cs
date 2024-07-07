using System.Text.Json.Serialization;
using InventoryAPI.DbContext;
using InventoryAPI.Middleware;
using InventoryAPI.Models;
using InventoryAPI.Models.Requests;
using InventoryAPI.Requests;
using InventoryAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Authentication services
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPasswordHasher<AuthenticationRequest>, PasswordHasher<AuthenticationRequest>>();

builder.Services.AddScoped<IInventoryService, InventoryService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "InventoryAPI",
        Description = "API that provides the means to modify an inventory. Only authenticated users can only modify their own inventory, where each user has their own inventory, with associated items.",
        Version = "v1" }
    );
}); 

// Register the regex constraint for Redocly
builder.Services.Configure<RouteOptions>(options =>
{
    options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

app.UseMiddleware<JwtMiddleware>();

// User endpoints
app.MapPost("/user/create", async (CreateUserRequest createUserRequest, IUserService userService) =>
{
    try
    {
        int? user = await userService.CreateUserAsync(createUserRequest);

        if (user == null) return Results.BadRequest();

        return Results.Ok(user);
    }
    catch (DbUpdateException)
    {
        return Results.Conflict();
    }
}).WithName("Create user")
.WithDescription("Creates a user provided a first name, surname, username and password.")
.Produces(400).Produces(200);

// Authenticate user: note of authorization response header; add that to other endpoints which require authentication
app.MapPost("/user/authenticate", async (AuthenticationRequest authenticationRequest,
    IUserService userService, HttpContext httpContext) =>
{
    var authenticatedUserLogin = await userService.LogInAsync(authenticationRequest);

    if (authenticatedUserLogin == null) return Results.Unauthorized();

    // Add the access token to the requester's response headers. Lasts an hour. Add this to the request header for other endpoints to make authenticated requests for your user
    httpContext.Response.Headers.Append("Authorization", authenticatedUserLogin.AccessToken);

    return Results.Ok(authenticatedUserLogin);
}).WithName("Authenticate user")
.WithDescription("Authenticates a user assuming the provided username and password is correct.")
.Produces(401).Produces(200);

app.MapGet("/user/refresh", async (int userId, string refreshToken,
    IUserService userService, ITokenService tokenService) =>
{
    if (!await userService.IsRefreshTokenValidAsync(refreshToken)) return Results.BadRequest();

    // If it is valid, generate a new access token which will be used by client to make further requests
    return Results.Ok(tokenService.CreateAccessToken(userId));
}).WithName("Generate new access token")
.WithDescription("Generates a new access token (lasting an hour) which is used to make further authenticated requests.")
.Produces(400).Produces(200);

// Inventory endpoints
// Adding a single item to a user's inventory
app.MapPost("/inventory/item", async (HttpContext httpContext, 
    ItemRequest itemRequest, IInventoryService inventoryService) =>
{
    // Ensure that the authenticated user can only modify their own inventory
    if (httpContext.Items["User"] is not User user) return Results.Unauthorized();

    var result = await inventoryService.AddItemAsync(user.Id, itemRequest);

    if (!result) return Results.BadRequest();

    return Results.Ok(itemRequest);
}).WithName("Add single item")
.WithDescription("Adds a single item to a user's inventory")
.Produces(400).Produces(200);

app.MapPost("/inventory/items", async (HttpContext httpContext,
    ItemRequest[] itemRequest, IInventoryService inventoryService) =>
{
    // Ensure that the authenticated user can only modify their own inventory
    if (httpContext.Items["User"] is not User user) return Results.Unauthorized();

    var result = await inventoryService.AddItemsAsync(user.Id, itemRequest);

    if (!result) return Results.BadRequest();

    return Results.Ok(itemRequest);
}).WithName("Add multiple items")
.WithDescription("Adds multiple items to a user's inventory. If one item is invalid, the entire transaction is rolled back.")
.Produces(400).Produces(200);

app.MapGet("/inventory/items", async (HttpContext httpContext,
    IInventoryService inventoryService) =>
{
    // Ensure that the authenticated user can only modify their own inventory
    if (httpContext.Items["User"] is not User user) return Results.Unauthorized();

    var result = await inventoryService.GetUserInventoryAsync(user.Id);

    if (!result.Any()) return Results.BadRequest();

    return Results.Ok(result);
}).WithName("Fetch user inventory")
.WithDescription("Adds multiple items to an authenticated user's inventory. If one item is invalid, the entire transaction is rolled back.")
.Produces(400).Produces(200);

app.MapDelete("/inventory/item", async (HttpContext httpContext,
    [FromBody] ItemSubtractRequest itemSubtractRequest, IInventoryService inventoryService) =>
{
    // Ensure that the authenticated user can subtract their own inventory
    if (httpContext.Items["User"] is not User user) return Results.Unauthorized();

    var result = await inventoryService.SubtractItemAsync(user.Id, itemSubtractRequest);

    if (!result.Any()) return Results.BadRequest();

    return Results.Ok(result);
}).WithName("Subtract item from user inventory")
.WithDescription("Subtracts an item from an authenticated user's inventory")
.Produces(400).Produces(200);

app.UseSwagger();

app.UseReDoc(c =>
{
    c.SpecUrl = "/swagger/v1/swagger.json";
    c.RoutePrefix = "redoc";
});

// Redirect root URL to ReDoc when deployed via Docker 
#if !DEBUG
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/redoc");
        return;
    }

    await next();
});
#endif

app.Run();

[JsonSerializable(typeof(IEnumerable<Inventory>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}