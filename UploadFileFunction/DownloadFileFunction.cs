using System.Net;
using System.Threading.Tasks;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace UploadFileFunction
{
    public class DownloadFileFunction
    {
        private readonly ILogger _logger;

        public DownloadFileFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DownloadFileFunction>();
        }

        [Function("DownloadFile")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            string directoryName = query["directory"];
            string fileName = query["fileName"];

            if (string.IsNullOrEmpty(fileName))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("File name is required");
                return badResponse;
            }

            try
            {
                var connectionString = Environment.GetEnvironmentVariable("StorageConnection")
                ?? throw new InvalidOperationException("StorageConnection is not configured.");
                string shareName = "dummycontracts";

                var serviceClient = new ShareServiceClient(connectionString);
                var shareClient = serviceClient.GetShareClient(shareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                var downloadInfo = await fileClient.DownloadAsync();

                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Disposition", $"attachment; filename={fileName}");
                await downloadInfo.Value.Content.CopyToAsync(response.Body);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error downloading file: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
