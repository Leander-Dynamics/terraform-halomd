namespace MPArbitration.Model
{
    public class MergeClaimsResult
    {
        public int Iterations { get; set; }
        public ArbitrationCase? MergedRecord { get; set; }
        public string Message { get; set; } = "";
    }
}
