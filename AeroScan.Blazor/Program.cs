using AeroScan.Blazor;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API 주소 — 나중에 배포할 때 변경
var apiBase = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7289/";
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBase) });

await builder.Build().RunAsync();