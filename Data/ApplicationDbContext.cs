using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SistemaGestionDeportiva.Models;

namespace SistemaGestionDeportiva.Data
{
    public class ApplicationDbContext : IdentityDbContext<Usuario>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Liga> Ligas { get; set; }
        public DbSet<Equipo> Equipos { get; set; }
        public DbSet<Jugador> Jugadores { get; set; }
        public DbSet<Entrenador> Entrenadores { get; set; }
        public DbSet<Partido> Partidos { get; set; }
        public DbSet<EstadisticaPartido> EstadisticasPartido { get; set; }
        public DbSet<Entrenamiento> Entrenamientos { get; set; }
        public DbSet<AsistenciaEntrenamiento> AsistenciasEntrenamiento { get; set; }
        public DbSet<MetricaEntrenamiento> MetricasEntrenamientos { get; set; }
        public DbSet<Gol> Goles { get; set; }
        public DbSet<EstadisticaJugador> EstadisticasJugador { get; set; }
        public DbSet<JugadorEstadisticas> JugadoresEstadisticas { get; set; }
        public DbSet<EquipoEstadisticas> EquiposEstadisticas { get; set; }
        public DbSet<AsistenciaEntrenamiento> AsistenciasEntrenamientos { get; set; }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Jugador || e.Entity is Equipo && e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in entries)
            {
                if (entry.Entity is Jugador jugador)
                {
                    var tieneGoles = await Set<Gol>().AnyAsync(g => g.JugadorId == jugador.JugadorId);
                    if (tieneGoles)
                    {
                        throw new InvalidOperationException($"No se puede eliminar el jugador {jugador.Nombre} porque tiene goles registrados");
                    }
                }
                else if (entry.Entity is Equipo equipo)
                {
                    var tieneGoles = await Set<Gol>().AnyAsync(g => g.EquipoId == equipo.EquipoId);
                    if (tieneGoles)
                    {
                        throw new InvalidOperationException($"No se puede eliminar el equipo {equipo.Nombre} porque tiene goles registrados");
                    }
                }
            }

            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<EstadisticaJugador>()
                .HasOne(e => e.Jugador)
                .WithMany(j => j.EstadisticasJugadores)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<EstadisticaJugador>()
                .HasOne(e => e.Partido)
                .WithMany(p => p.EstadisticasJugadores)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<EstadisticaJugador>()
                .HasOne(e => e.Equipo)
                .WithMany(e => e.EstadisticasJugadores)
                .OnDelete(DeleteBehavior.NoAction);
            // Configuraciones adicionales del modelo
            modelBuilder.Entity<Partido>()
                .HasOne(p => p.Liga)
                .WithMany(l => l.Partidos)
                .HasForeignKey(p => p.LigaId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Partido>()
                .HasOne(p => p.EquipoLocal)
                .WithMany(e => e.PartidosLocal)
                .HasForeignKey(p => p.EquipoLocalId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Partido>()
                .HasOne(p => p.EquipoVisitante)
                .WithMany(e => e.PartidosVisitante)
                .HasForeignKey(p => p.EquipoVisitanteId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configuración para EstadisticaPartido
            modelBuilder.Entity<EstadisticaPartido>()
                .HasOne(ep => ep.Partido)
                .WithMany(p => p.Estadisticas)
                .HasForeignKey(ep => ep.PartidoId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<EstadisticaPartido>()
                .HasOne(ep => ep.Jugador)
                .WithMany(j => j.Estadisticas)
                .HasForeignKey(ep => ep.JugadorId)
                .OnDelete(DeleteBehavior.Restrict); 

           
            modelBuilder.Entity<Equipo>()
                .HasMany(e => e.Jugadores)
                .WithOne(j => j.Equipo)
                .HasForeignKey(j => j.EquipoId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configurar relación Equipo-Entrenadores
            modelBuilder.Entity<Equipo>()
                .HasMany(e => e.Entrenadores)
                .WithOne(e => e.Equipo)
                .HasForeignKey(e => e.EquipoId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configurar relación Entrenador-Usuario (opcional)
            modelBuilder.Entity<Entrenador>()
                .HasOne(e => e.Usuario)
                .WithMany()
                .HasForeignKey(e => e.UsuarioId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Jugador>()
                .HasOne(j => j.Equipo)
                .WithMany(e => e.Jugadores)
                .HasForeignKey(j => j.EquipoId);

            // Configuración para la tabla Goles
            modelBuilder.Entity<Gol>(entity =>
            {
                entity.HasKey(g => g.GolId);
                // Relación con Partido (única que permitirá CASCADE)
                entity.HasOne(g => g.Partido)
                    .WithMany(p => p.Goles)
                    .HasForeignKey(g => g.PartidoId)
                    .OnDelete(DeleteBehavior.NoAction); 

                // Relación con Jugador (NO ACTION)
                entity.HasOne(g => g.Jugador)
                    .WithMany(j => j.Goles)
                    .HasForeignKey(g => g.JugadorId)
                    .OnDelete(DeleteBehavior.NoAction); 

                // Relación con Equipo (NO ACTION)
                entity.HasOne(g => g.Equipo)
                    .WithMany(e => e.Goles)
                    .HasForeignKey(g => g.EquipoId)
                    .OnDelete(DeleteBehavior.NoAction); 
            });
            modelBuilder.Entity<Entrenamiento>()
            .HasMany(e => e.Asistencias)
            .WithOne(a => a.Entrenamiento)
            .HasForeignKey(a => a.EntrenamientoId);

            modelBuilder.Entity<Entrenamiento>()
                .HasMany(e => e.Metricas)
                .WithOne(m => m.Entrenamiento)
                .HasForeignKey(m => m.EntrenamientoId);

            modelBuilder.Entity<AsistenciaEntrenamiento>()
                .HasOne(a => a.Jugador)
                .WithMany()
                .HasForeignKey(a => a.JugadorId);

            modelBuilder.Entity<Entrenamiento>()
        .HasOne(e => e.Liga)
        .WithMany()
        .HasForeignKey(e => e.LigaId)
        .OnDelete(DeleteBehavior.NoAction); 

            // Configuración para otras relaciones que puedan causar ciclos
            modelBuilder.Entity<MetricaEntrenamiento>()
                .HasOne(m => m.Jugador)
                .WithMany()
                .HasForeignKey(m => m.JugadorId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MetricaEntrenamiento>()
                .HasOne(m => m.Entrenamiento)
                .WithMany()
                .HasForeignKey(m => m.EntrenamientoId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<EquipoEstadisticas>()
                .HasKey(e => e.EquipoEstadisticasId); // Explicitamente define la clave primaria

            // Configuración para JugadorEstadisticas si también necesita clave primaria
            modelBuilder.Entity<JugadorEstadisticas>()
                .HasKey(j => j.JugadorEstadisticasId);

        }
    }
}