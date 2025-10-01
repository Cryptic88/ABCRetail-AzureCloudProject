using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ABCRetail.Services
{
    public class BlobFunctionService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseFunctionUrl;

        public BlobFunctionService(HttpClient httpClient, string baseFunctionUrl)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _baseFunctionUrl = baseFunctionUrl?.TrimEnd('/') ?? throw new ArgumentNullException(nameof(baseFunctionUrl));
        }

        public async Task<string> UploadsAsync(Stream fileStream, string fileName, string productId)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StreamContent(fileStream), "file", fileName);
            content.Add(new StringContent(productId), "productId");

            var response = await _httpClient.PostAsync($"{_baseFunctionUrl}/UploadsAsync", content);
            response.EnsureSuccessStatusCode();

            var blobUrl = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Blob URL returned from function: {blobUrl}");
            return blobUrl;

           // var imageUrl = (await response.Content.ReadAsStringAsync()).Trim().Trim('"');
           // return imageUrl; // This is the blob URL
        }

        public async Task DeleteBlobAsync(string blobNameOrUrl)
        {
            var response = await _httpClient.DeleteAsync($"{_baseFunctionUrl}/DeleteBlobAsync?blobName={blobNameOrUrl}");
            response.EnsureSuccessStatusCode();
        }
    }
}