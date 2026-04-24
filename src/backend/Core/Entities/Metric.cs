using System.ComponentModel.DataAnnotations.Schema;

namespace DecisionEngine.Core.Entities
{
    public class Metric
    {
        public int Id { get; set; }

        [Column("campaign_id")]
        public int CampaignId { get; set; }

        public decimal Spend { get; set; }
        public decimal Revenue { get; set; }
        public decimal Roas { get; set; }
        public decimal Ctr { get; set; }

        [Column("recorded_at")]
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Campaign? Campaign { get; set; }
    }
}
