namespace MPArbitration.Model
{
    public enum CaseWorkflowAction
    {
        AssignCustomer = 3,
        AssignUser = 2,
        MarkRead = 0,
        MarkUnread = 1,
        NSARequestSentToPayor = 4,
        NotificationFailed = 5
    }

    public class CaseWorkflowParams
    {
        public int assignToId { get; set; }
        public int caseId { get; set; }
        public int customerId { get; set; }
        public CaseWorkflowAction action { get; set; }
        public string? message { get; set; }
        public string? JSON { get; set; } = null;
    }
}
