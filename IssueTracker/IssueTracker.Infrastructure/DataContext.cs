using IssueTracker.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace IssueTracker.Infrastructure.Data
{
    public class DataContext : DbContext
    {
        // NOTA: Deve esserci <DataContext> dopo DbContextOptions!
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        } 

        // Questa riga dice ad Entity Framework di creare una tabella denominata "Tickets" basata sul modello "Ticket"
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Qui possiamo configurare regole specifiche per il database, ad esempio valori di default o vincoli
            modelBuilder.Entity<Ticket>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Title).IsRequired().HasMaxLength(150);
                entity.Property(t => t.Description).IsRequired();

                // Salviamo le enumerazioni nel database come stringhe leggibili (es. "InProgress" invece di 1)
                entity.Property(t => t.Status)
                    .HasConversion(
                        v => v.ToString(),
                        v => (TicketStatus)Enum.Parse(typeof(TicketStatus), v));

                entity.Property(t => t.Priority)
                    .HasConversion(
                        v => v.ToString(),
                        v => (TicketPriority)Enum.Parse(typeof(TicketPriority), v));
            });

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Author)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.AuthorId)
                .OnDelete(DeleteBehavior.Restrict); // Evita conflitti di cascade delete

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}