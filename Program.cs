var builder = WebApplication.CreateBuilder(args);

// var portVar = Environment.GetEnvironmentVariable("PORT");

// if (portVar is {Length: >0} && int.TryParse(portVar, out int port))
// {
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5297);
    options.ListenAnyIP(7145);
    options.ListenAnyIP(8080);
});
// }

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
