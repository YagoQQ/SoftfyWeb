namespace SoftfyWeb.Modelos.Dtos
{
    public class MeGustaRespuestaDto
    {
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int Total { get; set; }
        public List<CancionMeGustaDto> Canciones { get; set; }
    }
}