using ABCRetail.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ABCRetail.Services
{
    public class AzureFileShareService
    {
        private readonly HttpClient _httpClient;
        private readonly string _uploadFunctionUrl;
        private readonly string _listFunctionUrl;
        private readonly string _downloadFunctionUrl;

        public AzureFileShareService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _uploadFunctionUrl = "https://fileshare-d3aaabdacyascnbe.southafricanorth-01.azurewebsites.net/api/UploadToFileShare";
            _listFunctionUrl = "https://fileshare-d3aaabdacyascnbe.southafricanorth-01.azurewebsites.net/api/ListFilesInDirectory";
            _downloadFunctionUrl = "https://fileshare-d3aaabdacyascnbe.southafricanorth-01.azurewebsites.net/api/DownloadFile";
        }

        public async Task UploadFileAsync(string directoryName, string fileName, Stream fileStream)
        {
            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(directoryName ?? ""), "directory");

            var response = await _httpClient.PostAsync(_uploadFunctionUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload file: {response.StatusCode} - {error}");
            }
        }

        public async Task<Stream> DownloadFileAsync(string directoryName, string fileName)
        {
            var requestUrl = $"{_downloadFunctionUrl}?directory={Uri.EscapeDataString(directoryName)}&fileName={Uri.EscapeDataString(fileName)}";

            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to download file: {response.StatusCode} - {error}");
            }

            return await response.Content.ReadAsStreamAsync();
        }

        public async Task<List<FileModel>> ListFilesAsync(string directoryName)
        {
            var requestUrl = $"{_listFunctionUrl}?directory={Uri.EscapeDataString(directoryName)}";

            var response = await _httpClient.GetAsync(requestUrl);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to list files: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var files = JsonSerializer.Deserialize<List<FileModel>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return files ?? new List<FileModel>();
        }
    }
}

