using Microsoft.EntityFrameworkCore;

namespace MPExternalDisputeAPI.Model
{
    public class DisputeIdrDbContext : DbContext
    {
        public DbSet<DisputeMaster> DisputeMaster { get; set; }
        public DbSet<DisputeCPT> DisputeCPT { get; set; }
        public DbSet<REF_CertifiedEntity> REF_CertifiedEntity { get; set; }
        public DbSet<RPT_EmailedFeePaymentRequests> RPT_EmailedFeePaymentRequests { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public DisputeIdrDbContext(DbContextOptions<DisputeIdrDbContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<RPT_EmailedFeePaymentRequests>()
                .HasNoKey();

            // If you have other configurations, you can include them here
        }

    }
}
