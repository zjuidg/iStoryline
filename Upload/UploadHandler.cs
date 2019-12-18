using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Nancy;

// https://bytefish.de/blog/file_upload_nancy/
namespace StorylineBackend.upload
{
    public class FileUploadResult
    {
        public string Identifier { get; set; }
    }
    
    public interface IFileUploadHandler
    {
        Task<FileUploadResult> HandleUpload(Stream stream);
    }
    
    public class UploadHandler: IFileUploadHandler
    {
        private readonly IConfiguration _applicationSettings;
        private readonly IRootPathProvider _rootPathProvider;

        public UploadHandler(IConfiguration applicationSettings, IRootPathProvider rootPathProvider)
        {
            _applicationSettings = applicationSettings;
            _rootPathProvider = rootPathProvider;
        }

        public async Task<FileUploadResult> HandleUpload(Stream stream)
        {
            string uuid = GetFileName();
            string targetFile = GetTargetFile(uuid);
            
            using (FileStream destinationStream = File.Create(targetFile))
            {
                await stream.CopyToAsync(destinationStream);
            }

            var md5 = CalculateMD5(targetFile);
            string md5File = GetTargetFile(md5);
            if (File.Exists(md5File))
            {
                File.Delete(targetFile);
            }
            else
            {
                // rename to md5
                File.Move(targetFile, md5File);
            }

            return new FileUploadResult
            {
                Identifier = md5
            };
        }
        
        private string GetTargetFile(string fileName)
        {
            return Path.Combine(GetUploadDirectory(), fileName);
        }

        private string GetFileName()
        {
            return Guid.NewGuid().ToString();
        }
        
        private string GetUploadDirectory()
        {
            var uploadDirectory = Path.Combine(_rootPathProvider.GetRootPath(), _applicationSettings["UploadDirectory"]);

            if (!Directory.Exists(uploadDirectory))
            {
                Directory.CreateDirectory(uploadDirectory);
            }

            return uploadDirectory;
        }
        
        private static string CalculateMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}