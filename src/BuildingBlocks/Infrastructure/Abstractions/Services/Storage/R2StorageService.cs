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

        public R2StorageService(IConfiguration configuration)
        {
            var accessKeyId = configuration["R2:AccessKeyId"] ?? throw new ArgumentNullException("R2:AccessKeyId");
            var secretAccessKey = configuration["R2:SecretAccessKey"] ?? throw new ArgumentNullException("R2:SecretAccessKey");
            _accountId = configuration["R2:AccountId"] ?? throw new ArgumentNullException("R2:AccountId");
            _bucketName = configuration["R2:BucketName"] ?? throw new ArgumentNullException("R2:BucketName");
            var endpoint = configuration["R2:Endpoint"] ?? $"https://{_accountId}.r2.cloudflarestorage.com";

            var config = new AmazonS3Config
            {
                ServiceURL = endpoint,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(accessKeyId, secretAccessKey, config);
        }

        public async Task<UploadR2Response> UploadFileAsync(UploadR2Request request)
        {
            try
            {
                var fileKey = string.IsNullOrEmpty(request.Folder)
                    ? request.FileName
                    : $"{request.Folder}/{request.FileName}";

                using (var stream = new MemoryStream(request.FileBytes))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _bucketName,
                        Key = fileKey,
                        InputStream = stream,
                        ContentType = request.ContentType,
                        AutoCloseStream = true
                    };

                    var response = await _s3Client.PutObjectAsync(putRequest);

                    if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception($"Failed to upload file to R2. Status: {response.HttpStatusCode}");
                    }

                    // Generate public URL (if bucket has public access configured)
                    var fileUrl = $"https://pub-{_accountId}.r2.dev/{fileKey}";

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
