using grpcServer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();

var app = builder.Build();

app.UseStaticFiles();

app.MapGrpcService<MessageManager>();
app.MapGrpcService<FileTransportService>();
app.Run();