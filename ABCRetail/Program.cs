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
                client.BaseAddress = new Uri("https://storeintable-amd0eqhbgtfpc4fy.southafricanorth-01.azurewebsites.net/api");
            });

            // Blob storage via Azure Functions
            builder.Services.AddHttpClient<BlobFunctionService>(client =>
            {
                client.BaseAddress = new Uri("https://uploadblob-akdbe9gjehfbcnfz.southafricanorth-01.azurewebsites.net/api");
            });

            builder.Services.AddHttpClient<QueueFunctionService>(client =>
            {
                client.BaseAddress = new Uri("https://processorder-dda4aqf4erf6b8fh.southafricanorth-01.azurewebsites.net/api/");
            });
            builder.Services.AddSingleton<QueueFunctionService>();

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
