namespace SoftfyWeb.Modelos
{
    public class PlaylistCancion
    {
        public int PlaylistId { get; set; }
        public Playlist Playlist { get; set; }

        public int CancionId { get; set; }
        public Cancion Cancion { get; set; }
    }
}
