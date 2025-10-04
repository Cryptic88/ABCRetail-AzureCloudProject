using ABCRetail.Services;

namespace ABCRetail
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            //Table storage
            // Table storage HTTP client
            builder.Services.AddHttpClient<TableStorageFunctionService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:7152/api"); // Local function host URL
            });

            // Register TableStorageFunctionService as a singleton
            builder.Services.AddSingleton<TableStorageFunctionService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(TableStorageFunctionService));
                return new TableStorageFunctionService(httpClient, "http://localhost:7152/api");
            });

            // Blob storage via Azure Functions
            builder.Services.AddHttpClient<BlobFunctionService>(client =>
            {
                client.BaseAddress = new Uri("http://localhost:7135/api"); // URL of your local Functions host
            });

            // Register BlobFunctionService as a singleton
            builder.Services.AddSingleton<BlobFunctionService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(BlobFunctionService));
                return new BlobFunctionService(httpClient, "http://localhost:7135/api"); // Function App URL
            });


            // Add HttpClient for QueueFunctionService
            builder.Services.AddHttpClient<QueueFunctionService>();

            // Add QueueFunctionService with base URL from appsettings.json
            builder.Services.AddSingleton(sp =>
            {
                var httpClient = sp.GetRequiredService<HttpClient>();
                var functionUrl = builder.Configuration["QueueFunctionUrl"]; // put the function URL in appsettings.json
                return new QueueFunctionService(httpClient, functionUrl);
            });

            builder.Services.AddHttpClient<AzureFileShareService>();

            builder.Services.AddSingleton<AzureFileShareService>(sp =>
            {
                var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient(nameof(AzureFileShareService));
                return new AzureFileShareService(httpClient);
            });

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

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
