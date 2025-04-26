using Microsoft.AspNetCore.SignalR;

namespace SistemaGestionDeportiva.Hubs
{
    public class PartidoHub : Hub
    {
        public async Task ActualizarMarcador(int partidoId, int golesLocal, int golesVisitante)
        {
            await Clients.All.SendAsync("RecibirActualizacion", partidoId, golesLocal, golesVisitante);
        }
    }
}