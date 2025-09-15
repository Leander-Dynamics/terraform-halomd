using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;


namespace MPArbitration.Model
{
    
    public class AuthorityDisputeWorkItem
    {
        [JsonPropertyName("assignedUser")]
        public string AssignedUser { get; set; } = "";

        [JsonPropertyName("disputeId")]
        public int DisputeId { get; set; } = 0;

        [JsonPropertyName("note")]
        public AuthorityDisputeNote? Note { get; set; } = null;

        [JsonPropertyName("workQueue")]
        public WorkQueueName WorkQueue { get; set; } = WorkQueueName.None;
    }
    
}
