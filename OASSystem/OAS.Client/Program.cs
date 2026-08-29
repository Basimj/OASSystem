using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OAS.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddClientServices(builder.HostEnvironment.BaseAddress);

await builder.Build().RunAsync();
