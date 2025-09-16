using System.ComponentModel.DataAnnotations;

namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to handle Dispute log 
    /// </summary>
    public class DisputeLog
    {
        /// <summary>
        /// Used to hold ChangeLogID
        /// </summary>
        [Key]
        public int ChangeLogID { get; set; }

        /// <summary>
        /// Used to hold TransactionID
        /// </summary>
        public int TransactionID { get; set; }

        /// <summary>
        /// Used to hold Entity name where the activity done
        /// </summary>
        public string? TableName { get; set; }

        /// <summary>
        /// Used to hold Activity type
        /// </summary>
        public string? Activity { get; set; }


        /// <summary>
        /// Used to hold PreviousValue
        /// </summary>
        public string? PreviousValue { get; set; }

        /// <summary>
        /// Used to hold NewValue
        /// </summary>
        public string? NewValue { get; set; }

        /// <summary>
        /// Used to hold CreatedBy
        /// </summary>
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Used to hold CreatedOn
        /// </summary>
        public DateTime? CreatedDate { get; set; }
    }
}
