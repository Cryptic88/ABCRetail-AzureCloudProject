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

            //Blob storage
            var connectionString2 = builder.Configuration.GetConnectionString("AzureStorage");
            builder.Services.AddSingleton(new BlobService(connectionString2));

            builder.Services.AddSingleton<QueueService>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
                return new QueueService(connectionString, "orders");
            });

            builder.Services.AddSingleton<AzureFileShareService>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("AzureStorage");
                return new AzureFileShareService(connectionString, "fileshare");
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
