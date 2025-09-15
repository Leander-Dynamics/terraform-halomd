namespace MPArbitration.Model
{
    /// <summary>
    /// Entity to define the API response structure
    /// </summary>
    public class APIResponse
    {
        /// <summary>
        /// Status code of the respnse 
        /// </summary>
        public int statusCode { get; set; }

        /// <summary>
        /// Respnse message
        /// </summary>
        public string? message { get; set; }

        /// <summary>
        /// Response data's
        /// </summary>
        public object? data { get; set; }

    }

    /// <summary>
    /// Entity to define response messages
    /// </summary>
    public static class StatusMessage
    {
        /// <summary>
        /// Success
        /// </summary>
        public static string Success { get { return "Success"; } }

        /// <summary>
        /// Not success
        /// </summary>
        public static string NotSuccess { get { return "Failed"; } }

        /// <summary>
        /// InvalidRequest
        /// </summary>
        public static string InvalidRequest { get { return "Invalid Request."; } }
    }
}
