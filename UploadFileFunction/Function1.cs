using System.Net;
using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace UploadFileFunction;
public class UploadToFileShareFunction
{
    private readonly ILogger _logger;

    public UploadToFileShareFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UploadToFileShareFunction>();
    }

    [Function("UploadToFileShare")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            // Get boundary from content-type header
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Missing Content-Type header");
                return bad;
            }

            var contentType = contentTypeValues.First();
            var boundary = HeaderUtilities.RemoveQuotes(
                MediaTypeHeaderValue.Parse(contentType).Boundary
            ).Value;

            if (string.IsNullOrEmpty(boundary))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("Missing boundary in multipart form");
                return bad;
            }

            //(stackoverflow, 2020)
            var reader = new MultipartReader(boundary, req.Body);
            MultipartSection section;
            string fileName = null;
            MemoryStream fileStream = null;

            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                    section.ContentDisposition, out var contentDisposition
                );

                if (hasContentDispositionHeader && contentDisposition.DispositionType.Equals("form-data") &&
                    !string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    fileName = contentDisposition.FileName.Value;
                    fileStream = new MemoryStream();
                    await section.Body.CopyToAsync(fileStream);
                    fileStream.Position = 0;
                }
            }

            if (fileStream == null || string.IsNullOrEmpty(fileName))
            {
                var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                await bad.WriteStringAsync("No file uploaded");
                return bad;
            }

            // Upload to Azure File Share
            string connectionString = "DefaultEndpointsProtocol=https;AccountName=st10439009;AccountKey=REMOVED;EndpointSuffix=core.windows.net";
            string shareName = "dummycontracts";
            string directoryName = "dummycontracts";

            var serviceClient = new ShareServiceClient(connectionString);
            var shareClient = serviceClient.GetShareClient(shareName);
            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();

            var fileClient = directoryClient.GetFileClient(fileName);
            await fileClient.CreateAsync(fileStream.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync($"File '{fileName}' uploaded successfully.");
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed");
            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync($"Error uploading file: {ex.Message}");
            return error;
        }
    }

}

/* Reference List: 
 stackoverflow, 2020. Uploading file to azure file share from azure function app. [online] Available at:
 <https://stackoverflow.com/questions/64667114/uploading-file-to-azure-file-share-from-azure-function-app> [Accessed 4 October 2025].*/