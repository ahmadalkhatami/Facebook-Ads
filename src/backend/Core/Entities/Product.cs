using System.ComponentModel.DataAnnotations.Schema;

namespace DecisionEngine.Core.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }

        [Column("source_url")]
        public string SourceUrl { get; set; } = string.Empty;

        public decimal Rating { get; set; }

        [Column("sold_count")]
        public int SoldCount { get; set; }

        public int Score { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "SCRAPED";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
