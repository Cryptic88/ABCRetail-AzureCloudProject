using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace WriteToBlobFunction;

public class UploadProductImage
{
    private readonly ILogger<UploadProductImage> _logger;
    private readonly string _containerName = "products";

    public UploadProductImage(ILogger<UploadProductImage> logger)
    {
        _logger = logger;
    }

    [Function("UploadsAsync")]
    public async Task<HttpResponseData> UploadsAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        FunctionContext context)
    {
        try
        {
            var logger = context.GetLogger("UploadsAsync");

            // Get content-type header
            var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
            if (string.IsNullOrEmpty(contentType))
                throw new System.Exception("Missing Content-Type header.");

            var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
            if (string.IsNullOrEmpty(boundary))
                throw new System.Exception("Missing boundary in multipart form-data.");

            var reader = new MultipartReader(boundary, req.Body);

            var section = await reader.ReadNextSectionAsync();
            Stream fileStream = null;
            string fileName = null;
            string productId = null;

            while (section != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();
                if (contentDisposition.IsFileDisposition())
                {
                    fileName = contentDisposition.FileName.Value;
                    fileStream = new MemoryStream();
                    await section.Body.CopyToAsync(fileStream);
                    fileStream.Position = 0;
                }
                else if (contentDisposition.IsFormDisposition())
                {
                    var key = contentDisposition.Name.Value;
                    using var reader2 = new StreamReader(section.Body);
                    var value = await reader2.ReadToEndAsync();
                    if (key == "productId") productId = value;
                }

                section = await reader.ReadNextSectionAsync();
            }

            if (fileStream == null || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(productId))
                throw new System.Exception("File or Product ID missing.");

            // Upload to blob
            // (Microsoft Learn, 2025)
            var connectionString = Environment.GetEnvironmentVariable("StorageConnection")
                ?? throw new InvalidOperationException("StorageConnection is not configured.");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.UploadAsync(fileStream, overwrite: true);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync(blobClient.Uri.ToString());
            logger.LogInformation($"File uploaded: {fileName}");
            return response;
        }
        catch (System.Exception ex)
        {
            var response = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await response.WriteStringAsync($"Error uploading file: {ex.Message}");
            _logger.LogError(ex.Message);
            return response;
        }
    }

    [Function("DeleteBlobAsync")]
    public async Task<HttpResponseData> DeleteBlobAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "delete")] HttpRequestData req)
    {
        try
        {
            var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var blobName = query["blobName"];

            if (string.IsNullOrWhiteSpace(blobName))
            {
                var badResponse = req.CreateResponse(System.Net.HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Blob name is required.");
                return badResponse;
            }

            var connectionString = Environment.GetEnvironmentVariable("StorageConnection")
                ?? throw new InvalidOperationException("StorageConnection is not configured.");
            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync(Azure.Storage.Blobs.Models.DeleteSnapshotsOption.IncludeSnapshots);

            var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await response.WriteStringAsync($"Blob '{blobName}' deleted successfully.");
            _logger.LogInformation($"Blob deleted: {blobName}");
            return response;
        }
        catch (System.Exception ex)
        {
            _logger.LogError($"Error deleting blob: {ex.Message}");
            var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error deleting blob: {ex.Message}");
            return errorResponse;
        }
    }
}

/*Reference List:
 
 Microsoft Learn, 2025. Tutorial: Upload and analyze a file with Azure Functions and Blob Storage. [online] Available at:
 <https://learn.microsoft.com/en-us/azure/storage/blobs/blob-upload-function-trigger?tabs=azure-portal> [Accessed 1 October 20025].

 */