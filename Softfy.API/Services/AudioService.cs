using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace SoftfyWeb.Services
{
    public class AudioService
    {
        private readonly Cloudinary _cloudinary;

        public AudioService(Cloudinary cloudinary)
        {
            _cloudinary = cloudinary;
        }

        public async Task<RawUploadResult> SubirAudioCloudinaryAsync(IFormFile archivo)
        {
            using var stream = archivo.OpenReadStream();
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(archivo.FileName, stream),
                Folder = "softfy/audios"
            };

            var result = await _cloudinary.UploadAsync(uploadParams);
            if (result.Error != null)
                throw new Exception(result.Error.Message);

            return result;
        }

        public async Task EliminarArchivoAsync(string url)
        {
            var publicId = ObtenerPublicIdDesdeUrl(url);
            if (publicId == null) return;

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Raw
            };

            await _cloudinary.DestroyAsync(deletionParams);
        }

        private string? ObtenerPublicIdDesdeUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var partes = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

                var indexUpload = Array.IndexOf(partes, "upload");
                if (indexUpload == -1 || indexUpload + 1 >= partes.Length)
                    return null;

                var partesId = partes.Skip(indexUpload + 1).ToArray();
                var nombreConExtension = Path.Combine(partesId);
                var extension = Path.GetExtension(nombreConExtension);
                return nombreConExtension.Replace(extension, "").Replace("\\", "/");
            }
            catch
            {
                return null;
            }
        }
    }
}
