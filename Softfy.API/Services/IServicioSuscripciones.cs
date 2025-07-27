using System.Threading.Tasks;


namespace Softfy.API.Services
{
    public interface IServicioSuscripciones
    {
        Task<bool> ActivarSuscripcionPorEmail(int planId, string email);
    }
}
