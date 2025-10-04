using ABCRetail.Models;
using System.Net;
using Azure.Storage.Files.Shares;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace UploadFileFunction
{
    public class ListFilesFunction
    {
        private readonly ILogger _logger;

        public ListFilesFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<ListFilesFunction>();
        }

        [Function("ListFilesInDirectory")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req)
        {
            try
            {
                string connectionString = Environment.GetEnvironmentVariable("AzureFilesConnectionString");
                string shareName = Environment.GetEnvironmentVariable("AzureFileShareName");
                string directoryName = "dummycontracts";

                var serviceClient = new ShareServiceClient(connectionString);
                var shareClient = serviceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);

                var files = new List<FileModel>();

                await foreach (var item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        var fileClient = directoryClient.GetFileClient(item.Name);
                        var properties = await fileClient.GetPropertiesAsync();

                        files.Add(new FileModel
                        {
                            Name = item.Name,
                            Size = properties.Value.ContentLength,
                            LastModified = properties.Value.LastModified
                        });
                    }
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(files));

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files");
                var error = req.CreateResponse(HttpStatusCode.InternalServerError);
                await error.WriteStringAsync($"Error listing files: {ex.Message}");
                return error;
            }
        }
    }
}
