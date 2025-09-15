namespace MPArbitration.Model
{
    /// <summary>
    /// 
    /// </summary>
    public interface INotificationDocument
    {
        /// <summary>
        /// 
        /// </summary>
        int ArbitrationCaseId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        
        string HTML { get; set; }
        /// <summary>
        /// 
        /// </summary>
        string JSON { get; set; }
        /// <summary>
        /// 
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// 
        /// </summary>
        NotificationType NotificationType { get; set; }
    }
}