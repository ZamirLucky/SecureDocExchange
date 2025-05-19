using FileStore.Filters;
using FileStore.ViewModels;
using LawyerApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text.Json;

namespace FileStore.Controllers
{

    [Authorize]
    [Route("[controller]")]
    public class FileUploadController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        private const string UploadFolder = "App_Data/SecureUploads";

        public FileUploadController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }


        [HttpGet("")]
        public IActionResult Upload()
        {
            return View(new FileUploadViewModel());
        }


        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(FileUploadViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var file = vm.File!;
            var clientEmail = vm.ClientEmail;
            var lawyerEmail = vm.LawyerEmail;

            // // magic-number check
            if (!await HasDocxMagicAsync(vm.File!))
            {
                ModelState.AddModelError(nameof(vm.File), "Only .docx files are allowed.");
                return View(vm);
            }

            //var file = Request.Form.Files.FirstOrDefault();
            //if (file == null || file.Length == 0)
            //    return BadRequest("No file uploaded.");

            // Ensure folder exists 
            var uploadRoot = Path.Combine(_environment.ContentRootPath, UploadFolder);
            Directory.CreateDirectory(uploadRoot);

            // Generate GUID code
            var code = Guid.NewGuid().ToString("N");

            // Save file under GUID-based name
            var savedFileName = $"{code}.docx";
            var savePath = Path.Combine(uploadRoot, savedFileName);
            await using (var fs = new FileStream(savePath, FileMode.CreateNew))
            {
                await file.CopyToAsync(fs);
            }

            // Compute SHA-256
            var hash = await ComputeSha256Async(savePath);
            await System.IO.File.WriteAllTextAsync(Path.ChangeExtension(savePath, ".sha256"), hash);

            // Persist mapping object
            var mapping = new
            {
                ClientEmail = clientEmail,
                LawyerEmail = lawyerEmail,
                Code = code,
                Sha256Hash = hash,
                UploadedAt = DateTime.UtcNow
            };

            // Determine metadata file path
            var metadataPath = Path.Combine(
                _environment.ContentRootPath, 
                "App_Data", "SecureUploads", "metadata.json"
            );

            // Load existing mappings (or new list)
            List<object> allMappings;
            if (System.IO.File.Exists(metadataPath))
            {
                allMappings = new List<object>();
            }
            else
            {
                var json = await System.IO.File.ReadAllTextAsync(metadataPath);

                if (string.IsNullOrEmpty(json))
                {
                    allMappings = new List<object>();
                }
                else
                {
                    try
                    {
                        allMappings = JsonSerializer.Deserialize<List<object>>(json) ?? new List<object>();
                    }
                    catch (JsonException)
                    {
                        allMappings = new List<object>();
                    }
                }
            }

             // Add the new entry
             allMappings.Add(mapping);

            // Save back to file
            var newJson = JsonSerializer.Serialize(
                allMappings, 
                new JsonSerializerOptions { WriteIndented = true }
            );

            await System.IO.File.WriteAllTextAsync(metadataPath, newJson);

            ViewBag.Code = code;    // show code back to user
            return View("UploadSuccess");
        }


        // Helper to inspect first 4 bytes for PK\003\004
        private static async Task<bool> HasDocxMagicAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            var header = new byte[4];
            var bytesRead = await stream.ReadAsync(header, 0, header.Length);

            // DOCX files signature
            return bytesRead == 4
                && header[0] == 0x50
                && header[1] == 0x4B
                && header[2] == 0x03
                && header[3] == 0x04;
        }

        // Helper to compute SHA-256 hash in hex
        private static async Task<string> ComputeSha256Async(string filePath)
        {
            await using var fs = System.IO.File.OpenRead(filePath);
            using var sha = SHA256.Create();
            var hashBytes = await sha.ComputeHashAsync(fs);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        [HttpGet("Download")]
        public IActionResult Download()
        {
            return View("Download");   
        }

        [ServiceFilter(typeof(DownloadAuthorizeAttribute))]
        [HttpPost("Download")]
        [ValidateAntiForgeryToken]
        public IActionResult Download(DownloadViewModel vm)
        {
            // Validate model
            if (!ModelState.IsValid)
                return View("Download", vm);

            // Build file path
            var code = Guid.Parse(vm.Code!);
            var fileName = $"{code.ToString("N")}.docx";
            var filePath = Path.Combine(
                _environment.ContentRootPath,
                UploadFolder,
                fileName);

            // Check existence
            if (!System.IO.File.Exists(filePath))
                return NotFound();

            // Read file bytes
            byte[] plain = System.IO.File.ReadAllBytes(filePath);

            // Generate AES key+IV
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();

            // Encrypt the file
            byte[] cipher;
            using (var ms = new MemoryStream())
            using (var encryptor = aes.CreateEncryptor())
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(plain, 0, plain.Length);
                cs.FlushFinalBlock();
                cipher = ms.ToArray();
            }

            // Encrypt AES Key with RSA (public key of lawyer)
            //    Load their certificate from store 
            using var rsa = RSA.Create();
            var publicKeyPem = PublicKeyProvider.GetPublicKeyPem();
            rsa.ImportFromPem(publicKeyPem);
            byte[] encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);

            // Build a response bundle:
            using var outMs = new MemoryStream();
            void WriteBlock(byte[] data)
            {
                outMs.Write(BitConverter.GetBytes(data.Length), 0, 4);
                outMs.Write(data, 0, data.Length);
            }
            WriteBlock(encryptedKey);
            WriteBlock(aes.IV);
            WriteBlock(cipher);

            // Return the file directly
            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName
            );

        }


    }

}
