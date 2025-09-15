using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MPExternalDisputeAPI.Model
{
    public class ArbitrationDbContext : DbContext
    {
        protected readonly IConfiguration Configuration;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ArbitrationDbContext(DbContextOptions<ArbitrationDbContext> options)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            : base(options)
        {

        }

        public DbSet<ArbitrationCase> ArbitrationCases { get; set; }
        public DbSet<Payor> Payors { get; set; }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<ClaimCPT> ClaimCPT { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // convert Status to string and back again instead of using integers as a backing store
            //builder.Entity<ArbitrationCase>()
            //    .Property(e => e.Status)
            //    .HasConversion(v => v.ToString(),
            //                    v => (ArbitrationStatus)Enum.Parse(typeof(ArbitrationStatus), v));

            #region UTC Date conversion in and out of the datastore
            var UtcDtConverter = new ValueConverter<DateTime?,  DateTime?>(
                from => !from.HasValue ? from : (from.Value.Kind != DateTimeKind.Unspecified ? from.Value.ToUniversalTime() : DateTime.SpecifyKind(from.Value, DateTimeKind.Utc)),
                to => to.HasValue ? DateTime.SpecifyKind(to.Value, DateTimeKind.Utc) : to
            );
            #endregion

            #region Set up some indexes EF can't seem to figure out
            #endregion

            #region Set up some relationships EF can't seem to figure out

            #endregion

            #region Create a special-purpose utility model
            #endregion
        }

    }
}
