using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using librsync.net;
using LunarUploader.Misc;
using LunarUploader.VRChatApi.Models;

namespace LunarUploader.VRChatApi 
{
    public static class CustomApiFileHelper 
    {
        private const int MultipartBufferSize = 10 * 1024 * 1024;

        private static byte[] _fileAsBytes;
        private static byte[] _sigFileAsBytes;
        private static CustomApiFile.FileMetadata _fileMetadata;
        private static readonly HttpClient _awsHttpClient = DownloadHelper.HttpClient;

        public static async Task<bool> UploadFile(VRChatApiClient client, string fileName, string friendlyName, string existingId = "", bool cleanUp = false, Action<CustomApiFile> onSuccess = null, Action<string> onFailure = null)
        {
            try 
            {
                Reset();

                _fileAsBytes = File.ReadAllBytes(fileName);

                var sigFileName = await CreateSignatureFile(fileName).ConfigureAwait(false);
                _fileMetadata = new CustomApiFile.FileMetadata() 
                {
                    FileMD5 = GetFileMD5AsBase64(),
                    FileSizeInBytes = GetLength(_fileAsBytes),
                    SignatureMD5 = GetSignatureMD5AsBase64(),
                    SignatureSizeInBytes = GetLength(_sigFileAsBytes)
                };

                var ext = Path.GetExtension(fileName);

                CustomApiFile apiFile = null;
                if (string.IsNullOrEmpty(existingId)) apiFile = await client.CustomApiFile.Create(friendlyName, GetMimeTypeFromExtension(ext), ext).ConfigureAwait(false);
                else apiFile = await client.CustomApiFile.Get(existingId).ConfigureAwait(false);

                var newApiFile = await apiFile.CreateNewVersion(CustomApiFile.FileType.Full, _fileMetadata).ConfigureAwait(false);

                var fdt = CustomApiFile.Version.FileDescriptor.Type.file;

                var fileDesc = newApiFile.GetFileDescriptor(newApiFile.GetLatestVersionNumber(), fdt);

                var successFile = false;
                switch (fileDesc.Category) 
                {
                    case CustomApiFile.Category.Simple:
                        successFile = await SingleFileUpload(client, newApiFile, ext, Convert.FromBase64String(_fileMetadata.FileMD5), _fileAsBytes, fdt).ConfigureAwait(false);
                        break;
                    case CustomApiFile.Category.Multipart:
                        successFile = await MultipartFileUpload(client, newApiFile, _fileAsBytes, fdt).ConfigureAwait(false);
                        break;
                }

                if (!successFile) return false;

                ext = Path.GetExtension(sigFileName);

                fdt = CustomApiFile.Version.FileDescriptor.Type.signature;

                fileDesc = newApiFile.GetFileDescriptor(newApiFile.GetLatestVersionNumber(), fdt);

                var successSig = false;
                switch (fileDesc.Category) 
                {
                    case CustomApiFile.Category.Simple:
                        successSig = await SingleFileUpload(client, newApiFile, ext, Convert.FromBase64String(_fileMetadata.SignatureMD5), _sigFileAsBytes, fdt).ConfigureAwait(false);
                        break;
                    case CustomApiFile.Category.Multipart:
                        successSig = await MultipartFileUpload(client, newApiFile, _sigFileAsBytes, fdt).ConfigureAwait(false);
                        break;
                }

                if (!successSig) return false;

                Reset();

                ForceGC();
                if (cleanUp) Cleanup(fileName, sigFileName);

                onSuccess?.Invoke(newApiFile);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                onFailure?.Invoke(ex.ToString());
            }

            return false;
        }

        private static async Task<bool> MultipartFileUpload(VRChatApiClient client, CustomApiFile newApiFile, byte[] data, CustomApiFile.Version.FileDescriptor.Type type) 
        {
            try 
            {
                using var memoryStream = new MemoryStream(data);

                var etags = new List<string>();
                var partsLength = (int)Math.Ceiling((double) data.Length / MultipartBufferSize) + 1;
                for (var i = 1; i < partsLength; i++) {
                    var buffer = new byte[i == partsLength - 1 ? memoryStream.Length - memoryStream.Position : MultipartBufferSize];
                    var bytesRead = await memoryStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                    var tempEtags = new List<string>();
                    var fileRecord = await newApiFile.StartMultipartUpload(type, i).ConfigureAwait(false);

                    var res = await client.CustomApiFile.UploadFilePart(_awsHttpClient, fileRecord.Url, buffer, tempEtags).ConfigureAwait(false);
                    etags.AddRange(tempEtags);
                    Console.WriteLine($"(MP) Upload progress: {i / (double)partsLength * 100}%");
                }

                Console.WriteLine($"(MP) Upload progress: {partsLength / (double)partsLength * 100}%");

                newApiFile = await newApiFile.FinishUpload(type, etags).ConfigureAwait(false);

                return true;
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        private static async Task<bool> SingleFileUpload(VRChatApiClient client, CustomApiFile newApiFile, string ext, byte[] md5AsBase64, byte[] data, CustomApiFile.Version.FileDescriptor.Type type)
        {
            try 
            {
                var fileRecord = await newApiFile.StartSimpleUpload(type).ConfigureAwait(false);

                var res = await client.CustomApiFile.UploadFile(_awsHttpClient, fileRecord.Url, GetMimeTypeFromExtension(ext), md5AsBase64, data).ConfigureAwait(false);

                newApiFile = await newApiFile.FinishUpload(type).ConfigureAwait(false);

                return true;
            } catch (Exception ex) 
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public static string GetMimeTypeFromExtension(string extension) 
        {
            switch (extension)
            {
                case ".vrcw":
                    return "application/x-world";

                case ".vrca":
                    return "application/x-avatar";

                case ".dll":
                    return "application/x-msdownload";

                case ".unitypackage":
                     return "application/gzip";

                case ".gz":
                    return "application/gzip";

                case ".jpg":
                    return "image/jpg";

                case ".png":
                    return "image/png";

                case ".sig":
                    return "application/x-rsync-signature";

                case ".delta":
                    return "application/x-rsync-delta";
            }

            Console.WriteLine("Unknown file extension for mime-type: " + extension);
            return "application/gzip";
        }

        private static string GetFileMD5AsBase64()
        {
            if (_fileAsBytes == null) return string.Empty;
            return GetMD5AsBase64FromBytes(_fileAsBytes);
        }

        private static string GetSignatureMD5AsBase64() 
        {
            if (_sigFileAsBytes == null) return string.Empty;
            return GetMD5AsBase64FromBytes(_sigFileAsBytes);
        }

        private static string GetMD5AsBase64FromBytes(byte[] input)
        {
            using var md5 = MD5.Create();
            return Convert.ToBase64String(md5.ComputeHash(input));
        }

        private static int GetLength(byte[] input)
        {
            return input?.Length ?? -1;
        }

        private static async Task<string> CreateSignatureFile(string fileName)
        {
            try 
            {
                using var memStream = new MemoryStream(_fileAsBytes);
                using var sigStream = Librsync.ComputeSignature(memStream);
                using var sigMemStream = new MemoryStream();

                await sigStream.CopyToAsync(sigMemStream).ConfigureAwait(false);

                _sigFileAsBytes = sigMemStream.GetBuffer();

                return Path.ChangeExtension(fileName, ".sig");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            ForceGC();

            return string.Empty;
        }

        private static void Reset() 
        {
            _fileAsBytes = null;
            _sigFileAsBytes = null;
            _fileMetadata = null;
        }

        private static void ForceGC()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }


        private static void Cleanup(string fileName = "", string sigFileName = "") 
        {
            try 
            {
                if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName)) File.Delete(fileName);
                if (!string.IsNullOrEmpty(sigFileName) && File.Exists(sigFileName)) File.Delete(sigFileName);
            }
            catch { }
        }
    }
}