using Amazon.S3;
using Amazon.S3.Model;
using Contracts.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Shared.DTOs.Storage;

namespace Infrastructure.Abstractions.Services.Storage
{
    public class R2StorageService : IR2StorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _accountId;
        private readonly string _publicDomain;

        public R2StorageService(IConfiguration configuration)
        {
            var accessKeyId = configuration["R2:AccessKeyId"] ?? throw new ArgumentNullException("R2:AccessKeyId");
            var secretAccessKey = configuration["R2:SecretAccessKey"] ?? throw new ArgumentNullException("R2:SecretAccessKey");
            _accountId = configuration["R2:AccountId"] ?? throw new ArgumentNullException("R2:AccountId");
            _bucketName = configuration["R2:BucketName"] ?? throw new ArgumentNullException("R2:BucketName");
            _publicDomain = configuration["R2:PublicDomain"] ?? throw new ArgumentNullException("R2:PublicDomain");
            var endpoint = configuration["R2:Endpoint"] ?? $"https://{_accountId}.r2.cloudflarestorage.com";

            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true,
                SignatureVersion = "4"
            };

            _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, config);
        }

        public async Task<UploadR2Response> UploadFileAsync(UploadR2Request request)
        {
            try
            {
                // Encode filename to handle spaces and special characters
                var encodedFileName = Uri.EscapeDataString(request.FileName);
                
                var fileKey = string.IsNullOrEmpty(request.Folder)
                    ? encodedFileName
                    : $"{request.Folder}/{encodedFileName}";

                using (var stream = new MemoryStream(request.FileBytes))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileKey,
                        InputStream = stream,
                        ContentType = request.ContentType,
                        AutoCloseStream = true,
                        DisablePayloadSigning = false,
                        UseChunkEncoding = false // Disable chunked encoding for R2
                    };

                    var response = await _s3Client.PutObjectAsync(putRequest);

                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Failed to upload file to R2. Status: {response.HttpStatusCode}");
                    }

                    // Generate public URL using configured public domain
                    var fileUrl = $"{_publicDomain}/{fileKey}";

                    return new UploadR2Response
                    {
                        FileUrl = fileUrl,
                        FileKey = fileKey,
                        Size = request.FileBytes.Length,
                        ContentType = request.ContentType
                    };
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error uploading file to R2: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string fileKey)
        {
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey
                };

                var response = await _s3Client.DeleteObjectAsync(deleteRequest);
                return response.HttpStatusCode == System.Net.HttpStatusCode.NoContent;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting file from R2: {ex.Message}", ex);
            }
        }

        public async Task<string> GetPresignedUrlAsync(string fileKey, int expirationMinutes = 60)
        {
            try
            {
                var request = new GetPreSignedUrlRequest
                {
                    BucketName = _bucketName,
                    Key = fileKey,
                    Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
                };

                return await Task.FromResult(_s3Client.GetPreSignedURL(request));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating presigned URL: {ex.Message}", ex);
            }
        }
    }
}
