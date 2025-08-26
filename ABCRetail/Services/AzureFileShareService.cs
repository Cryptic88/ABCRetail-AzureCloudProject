using ABCRetail.Models;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABCRetail.Services
{
    public class AzureFileShareService
    {

        private readonly string _connectionString;
        private readonly string _fileShareName;

        public AzureFileShareService(string connectionString, string fileShareName)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _fileShareName = fileShareName ?? throw new ArgumentNullException(nameof(fileShareName));
        }
        public async Task UploadFileAsync(string directoryName, string fileName, Stream fileStream)
        {
            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                await directoryClient.CreateIfNotExistsAsync();

                var fileClient = directoryClient.GetFileClient(fileName);
                await fileClient.CreateAsync(fileStream.Length);
                await fileClient.UploadRangeAsync(new HttpRange(0, fileStream.Length), fileStream);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to upload file to Azure File Share." + ex.Message, ex);
            }
        }

        public async Task<Stream> DownloadFileAsync(string directoryName, string fileName)
        {
            try
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);
                var fileClient = directoryClient.GetFileClient(fileName);

                var downloadInfo = await fileClient.DownloadAsync();
                return downloadInfo.Value.Content;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to download file from Azure File Share." + ex.Message, ex);
            }
        }

        public async Task<List<FileModel>> ListFilesAsync(string directoryName)
        {
            var contracts = new List<FileModel>();
            try 
            {
                var serviceClient = new ShareServiceClient(_connectionString);
                var shareClient = serviceClient.GetShareClient(_fileShareName);
                var directoryClient = shareClient.GetDirectoryClient(directoryName);

                await foreach (ShareFileItem item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        var fileClient = directoryClient.GetFileClient(item.Name);
                        var properties = await fileClient.GetPropertiesAsync();
                        contracts.Add(new FileModel
                        {
                            Name = item.Name,
                            Size = properties.Value.ContentLength,
                            LastModified = properties.Value.LastModified
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to list files in Azure File Share." + ex.Message, ex);
            }
            return contracts;
        }
    }
}
