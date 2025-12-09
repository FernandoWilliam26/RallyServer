var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();


builder.Services.AddCors(options => { /* ... */ });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
}

app.UseHttpsRedirection();

app.UseCors("PermitirTodo");

app.UseAuthorization();
app.UseDefaultFiles(); 
app.UseStaticFiles();  
app.MapControllers();
app.Run();