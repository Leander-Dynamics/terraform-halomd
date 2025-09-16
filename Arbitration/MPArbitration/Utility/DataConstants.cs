namespace MPArbitration.Utility
{
    /// <summary>
    /// Used to manage all constants, Enums and other common static values 
    /// </summary>
    public static class DataConstants
    {
        /// <summary>
        /// Enum to maintain status codes
        /// </summary>
        public enum StatusCodes
        {
            /// <summary>
            /// Success
            /// </summary>
            Success = 200,

            /// <summary>
            /// InvalidToken
            /// </summary>
            InvalidToken = 100,

            /// <summary>
            /// BadRequest
            /// </summary>
            BadRequest = 400,

            /// <summary>
            /// Not found
            /// </summary>
            NotFound = 404,

            /// <summary>
            /// Unauthorized
            /// </summary>
            Unauthorized = 401

        }
    }
}
