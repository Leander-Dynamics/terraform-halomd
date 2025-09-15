namespace MPExternalDisputeAPI.Model
{
    /// <summary>
    /// Reuquest input to given as a request input payload
    /// </summary>
    public class SearchInput
    {
        public string? CustomerName { get; set; }
        /// <summary>
        /// Used to get ProviderNpi input
        /// </summary>
        public string? ProviderNpi { get; set; }

        /// <summary>
        /// Used to get operating states input
        /// </summary>
        public string? OperatingState { get; set; }

        /// <summary>
        /// Used to get initiation date input
        /// </summary>
        public DateTime? InitiationDate { get; set; }

        /// <summary>
        /// Used to get dispute status input
        /// </summary>
        public string? DisputeStatus { get; set; }
    }
}
