using System.ComponentModel.DataAnnotations.Schema;

namespace DecisionEngine.Core.Entities
{
    public class DecisionLog
    {
        public int Id { get; set; }

        [Column("campaign_id")]
        public int CampaignId { get; set; }

        public string Action { get; set; } = string.Empty; // KILL, SCALE, REPLACE_CREATIVE
        public string Reason { get; set; } = string.Empty;

        [Column(TypeName = "jsonb")]
        public string? Metadata { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Campaign? Campaign { get; set; }
    }
}
