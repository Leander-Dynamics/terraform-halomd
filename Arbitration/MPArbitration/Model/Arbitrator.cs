using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration.Attributes;

namespace MPArbitration.Model
{
    // Arbitrator/Mediator/Certified Entity, i.e. a decision maker we will track statistically 
    [Microsoft.EntityFrameworkCore.Index(nameof(Email),IsUnique=true)]
    public class Arbitrator
    {
        [ForeignKey("ArbitratorId")]
        [JsonPropertyName("caseArbitrators")]
        public virtual List<CaseArbitrator> CaseArbitrators { get; set; } = new List<CaseArbitrator>(); 
        
        //[ForeignKey("ArbitratorId")]
        //[JsonPropertyName("disputes")]
        //public virtual List<AuthorityDispute> Disputes { get; set; } = new List<AuthorityDispute>();

        [ForeignKey("ArbitratorId")]
        [JsonPropertyName("fees")]
        public virtual List<ArbitratorFee> Fees { get; set; } = new List<ArbitratorFee>();

        [JsonPropertyName("id")]
        public int Id { get; set; } = 0;

        [JsonPropertyName("arbitratorType")]
        [Default(ArbitratorType.Arbitrator)]
        [Column(TypeName = "nvarchar(40)")]
        public ArbitratorType ArbitratorType { get; set; } = ArbitratorType.Arbitrator;

        [JsonPropertyName("eliminateForServices")]
        [StringLength(50)]
        public string EliminateForServices { get; set; } = ""; // comma-separated list of services e.g. IOM,PA

        [JsonPropertyName("email")]
        [StringLength(60)]
        public string Email { get; set; } = "";

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }  // if ARbitrator disappears from authority's list, mark them as inactive since we can't delete them
        
        [JsonPropertyName("isLastResort")]
        public bool IsLastResort { get; set; } // always eliminate unless there's no other option

        [JsonPropertyName("fixedFee")]
        public double FixedFee { get; set; } = 0;

        [JsonPropertyName("name")]
        [StringLength(60)]
        public string Name { get; set; } = "";

        [JsonPropertyName("mediatorBatchedFee")]
        public double MediatorBatchedFee { get; set; } = 0;

        [JsonPropertyName("mediatorFixedFee")]
        public double MediatorFixedFee { get; set; } = 0;

        [JsonPropertyName("notes")]
        [StringLength(255)]
        public string Notes { get; set; } = "";

        [JsonPropertyName("phone")]
        [StringLength(20)]
        public string Phone { get; set; } = "";

        [JsonPropertyName("statistics")]
        [StringLength(255)]
        public string Statistics { get; set; } = "";  // JSON object with stats per service line

        [JsonPropertyName("updatedBy")]
        [StringLength(60)]
        public string UpdatedBy { get; set; } = "";

        [JsonPropertyName("updatedOn")]
        public DateTime? UpdatedOn { get; set; } = null;

        public static Arbitrator? Map(string tdiString, out DateTime AssignedDate)
        {
            AssignedDate = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(tdiString))
                throw new ArgumentNullException(nameof(tdiString));

            // John Doe : jdoe@gmail.com : (555) 999-8282; Assigned date: 06/13/2022

            var parts = tdiString.Split(new char[] { ':', ';' });
            if (parts.Length < 3)
                throw new ArgumentException("Arbitrator string does not contain enough segments!");


            if (parts.Length == 5 && parts[3].Trim() == "Assigned date")
                DateTime.TryParse(parts[4].Trim(), out AssignedDate);

            
            return new Arbitrator
            {
                Email = parts[1].Trim(),
                Name = parts[0].Trim(),
                Phone = parts[2].Trim()
            };
        }

    }

    public class ImportArbitrators
    {
        public IEnumerable<ArbitratorUploadItem> Arbitrators { get; set; } = new List<ArbitratorUploadItem>();
    }

    public class ArbitratorUploadItem
    {
        public string name { get; set; } = "";

        public System.Text.Json.JsonElement? statistics { get; set; }
    }
}
