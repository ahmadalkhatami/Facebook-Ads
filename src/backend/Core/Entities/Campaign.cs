using System.ComponentModel.DataAnnotations.Schema;

namespace DecisionEngine.Core.Entities
{
    public class Campaign
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        [Column("fb_campaign_id")]
        public string FbCampaignId { get; set; } = string.Empty;

        public decimal Budget { get; set; }
        public decimal Spend { get; set; }
        public decimal Revenue { get; set; }
        public decimal Roas { get; set; }
        public decimal Ctr { get; set; }
        public string Status { get; set; } = "ACTIVE";

        [Column("product_price")]
        public decimal ProductPrice { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
