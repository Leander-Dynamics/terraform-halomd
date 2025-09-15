
namespace MPArbitration
{
    [Serializable]
    class ArbitException : Exception
    {
        public ArbitException() { }

        public ArbitException(string msg)
            : base(String.Format(msg))
        {

        }
    }
    class ArbitClaimException : ArbitException
    {
        public ArbitClaimException() { }

        public ArbitClaimException(string msg)
            : base(String.Format(msg))
        {

        }
    }
    class ArbitDisputeException : ArbitException
    {
        public ArbitDisputeException() { }

        public ArbitDisputeException(string msg)
            : base(String.Format(msg))
        {

        }
    }
    class ArbitUploadException : ArbitException
    {
        public ArbitUploadException() { }

        public ArbitUploadException(string msg)
            : base(String.Format(msg))
        {

        }
    }
    class DisputeUploadException : ArbitException
    {
        public DisputeUploadException() { }

        public DisputeUploadException(string msg)
            : base(String.Format(msg))
        {

        }
    }
}
