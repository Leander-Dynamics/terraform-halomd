namespace MPArbitration.Model
{
	public interface IAuthorityCase
	{
		string Authority { get; set; }
		string AuthorityCaseId { get; set; }
		string AuthorityStatus { get; set; }
		string IneligibilityAction { get; set; }
		string IneligibilityReasons { get; set; }
	}

	public class AuthorityCase : IAuthorityCase
	{
		public string Authority { get; set; } = "";
		public string AuthorityCaseId { get; set; } = "";
		public string AuthorityStatus { get; set; } = "";
		public string IneligibilityAction { get; set; } = "";
		public string IneligibilityReasons { get; set; } = "";
	}

	public class ArchiveCaseResult
    {
		public bool IsAlreadyArchived { get; set; }
		public bool ArchiveNeeded { get; set; }
		public bool ArchiveError { get; set; }
		public string Message { get; set; } = "";
		public bool WasNewRecordModified { get; set; }
	}
}
