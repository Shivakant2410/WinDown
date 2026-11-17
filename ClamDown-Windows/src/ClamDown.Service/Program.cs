using ClamDown.Core.Power;
using ClamDown.Service;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService(options => options.ServiceName = "ClamDown Service");
builder.Services.AddSingleton<WindowsPowerRequest>();
builder.Services.AddSingleton<IPowerRequest>(sp => sp.GetRequiredService<WindowsPowerRequest>());
builder.Services.AddSingleton<SessionController>();
builder.Services.AddHostedService<ControlPipeWorker>();
builder.Services.AddHostedService<SessionWorker>();
await builder.Build().RunAsync();
