namespace MPArbitration.Model
{
    /// <summary>
    /// Entity used for dispute custom search filter 
    /// </summary>
    public class DisputeSearchInput
    {
        /// <summary>
        /// Used to get dispute number input
        /// </summary>
        public string? DisputeNumber { get; set; }

        /// <summary>
        /// Used to get customer input
        /// </summary>
        public string? Customer { get; set; }

        /// <summary>
        /// Used to get dispute status input
        /// </summary>
        public string? DisputeStatus { get; set; }

        /// <summary>
        /// Used to get entity input
        /// </summary>
        public string? Entity { get; set; }

        /// <summary>
        /// Used to get certified entity input
        /// </summary>
        public string? CertifiedEntity { get; set; }

        /// <summary>
        /// Used to get BriefDueDate from  date
        /// </summary>
        public DateTime? BriefDueDateFrom { get; set; }

        /// <summary>
        /// Used to get BriefDueDate to  date
        /// </summary>
        public DateTime? BriefDueDateTo { get; set; }

        /// <summary>
        /// Used to get BriefDueDate to  date
        /// </summary>
        public string? BriefApprover { get; set; }

        /// <summary>
        /// Used to get provider NPI
        /// </summary>
        public string? EntityNPI { get; set; }

        /// <summary>
        /// Used to get arbitID
        /// </summary>
        public int? ArbitID { get; set; }

    }
}
