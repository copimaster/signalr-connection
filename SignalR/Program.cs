using Microsoft.EntityFrameworkCore;
using SignalR.Extensions;
using SignalR.Hubs;
using SignalR.Persistence;
using SignalR.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
builder.Services.AddTransient<IChatRoomService, ChatRoomService>();
builder.Services.AddTransient<IMessageService, MessageService>();
builder.Services.AddSignalR().AddAzureSignalR(builder.Configuration["Azure:SignalR:ConnectionString"]); // Configura Azure SignalR

builder.Services.AddCors(options => {
    options.AddPolicy("CORSPolicy", builder => {
        builder
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .SetIsOriginAllowed(hosts => true); //Allow any origin
        //.WithOrigins("http://localhost:3000"); // Allow specific origins
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SharedDb>();
builder.Services.AddSingleton<IAuthService, AuthService>();

// Configurar JWT
builder.Services.AddJwtAuthentication(builder.Configuration);

// Agregar autorización
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseSwagger();
    //app.UseSwaggerUI();
    //app.UseDeveloperExceptionPage();
} 
else {
    var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider");
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();  // Ejecuta solo las migraciones pendientes
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseDeveloperExceptionPage();

// Configurar el pipeline HTTP
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Map your SignalR hub
app.MapHub<Chathub>("/Chat");
app.UseCors("CORSPolicy");

app.Run();
