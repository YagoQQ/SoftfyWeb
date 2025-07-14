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
            if (string.IsNullOrEmpty(url))
                return;

            var publicId = ObtenerPublicIdDesdeUrl(url);

            if (string.IsNullOrEmpty(publicId))
                throw new Exception("No se pudo extraer el Public ID desde la URL.");

            var deletionParams = new DeletionParams(publicId)
            {
                ResourceType = ResourceType.Raw // si subiste archivos .mp3
            };

            var resultado = await _cloudinary.DestroyAsync(deletionParams);

            if (resultado.Result != "ok")
                throw new Exception($"Error desde Cloudinary: {resultado.Error?.Message}");
        }

        private string ObtenerPublicIdDesdeUrl(string url)
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;

            var uploadIndex = path.IndexOf("/upload/");
            if (uploadIndex < 0)
                return null;

            var afterUpload = path.Substring(uploadIndex + "/upload/".Length);

            var parts = afterUpload.Split('/', 2);
            if (parts.Length == 2 && parts[0].StartsWith("v") && long.TryParse(parts[0].Substring(1), out _))
                return parts[1];

            return afterUpload;
        }
    }
}
