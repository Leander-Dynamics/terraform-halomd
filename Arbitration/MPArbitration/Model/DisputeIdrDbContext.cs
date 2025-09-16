using Microsoft.EntityFrameworkCore;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle DisputeIdrDbContext
    /// </summary>
    public class DisputeIdrDbContext : DbContext
    {
        /// <summary>
        /// DBSet to hold DisputeMaster table data's
        /// </summary>
        public DbSet<DisputeMaster> DisputeMaster { get; set; }

        /// <summary>
        /// DBSet to hold DisputeCPT table data's
        /// </summary>
        public DbSet<DisputeCPT> DisputeCPT { get; set; }

        /// <summary>
        /// DBSet to hold REF_DisputeMasterCertifiedEntity table data's
        /// </summary>
        public DbSet<DisputeMasterCertifiedEntity> REF_DisputeMasterCertifiedEntity { get; set; }

        /// <summary>
        /// DBSet to hold REF_DisputeMasterDisputeStatus table data's
        /// </summary>
        public DbSet<DisputeMasterDisputeStatus> REF_DisputeMasterDisputeStatus { get; set; }

        /// <summary>
        /// DBSet to hold REF_DisputeMasterCustomer table data's
        /// </summary>
        public DbSet<DisputeMasterCustomer> REF_DisputeMasterCustomer { get; set; }

        /// <summary>
        /// DBSet to hold REF_DisputeMasterEntity table data's
        /// </summary>
        public DbSet<DisputeMasterEntity> REF_DisputeMasterEntity { get; set; }

        /// <summary>
        /// DBSet to hold REF_DisputeMasterServiceLine table data's
        /// </summary>
        public DbSet<DisputeMasterServiceLine> REF_DisputeMasterServiceLine { get; set; }


        /// <summary>
        /// DBSet to hold DisputeLog data's
        /// </summary>
        public DbSet<DisputeLog> XLog_ChangeLog { get; set; }


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        /// <summary>
        /// Construction to initialize objects
        /// </summary>
        /// <param name="options"></param>
        public DisputeIdrDbContext(DbContextOptions<DisputeIdrDbContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {

        }

        /// <summary>
        /// On model creating handler to set additional properties
        /// </summary>
        /// <param name="modelBuilder">ModelBuilder object</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<DisputeMasterCertifiedEntity>()
                .HasNoKey();

            modelBuilder
                .Entity<DisputeMasterDisputeStatus>()
                .HasNoKey();

            modelBuilder
              .Entity<DisputeMasterCustomer>()
              .HasNoKey();

            modelBuilder
             .Entity<DisputeMasterEntity>()
             .HasNoKey();

            modelBuilder
            .Entity<DisputeMasterServiceLine>()
            .HasNoKey();
        }
    }
}
