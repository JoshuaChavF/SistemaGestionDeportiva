using SistemaGestionDeportiva.Models;

namespace SistemaGestionDeportiva.Repositories
{

    public interface IRepository
    {
        List<JugadorEstadisticas> ObtenerEstadisticasJugadores();
        EquipoEstadisticas ObtenerEstadisticasEquipo();
        // Agrega otros métodos necesarios
    }
}
