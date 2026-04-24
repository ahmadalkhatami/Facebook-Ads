using System.ComponentModel.DataAnnotations.Schema;

namespace DecisionEngine.Core.Entities
{
    public class Creative
    {
        public int Id { get; set; }

        [Column("product_id")]
        public int ProductId { get; set; }

        public string Hook { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;

        [Column("video_url")]
        public string VideoUrl { get; set; } = string.Empty;

        public decimal Ctr { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Product? Product { get; set; }
    }
}
