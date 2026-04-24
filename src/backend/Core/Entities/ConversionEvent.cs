namespace DecisionEngine.Core.Entities
{
    public class ConversionEvent
    {
        public string EventName { get; set; } = string.Empty; // Purchase, AddToCart, Lead
        public long EventTime { get; set; } // Unix timestamp
        public string Email { get; set; } = string.Empty; // Hashed
        public string Phone { get; set; } = string.Empty; // Hashed
        public string ClientIpAddress { get; set; } = string.Empty;
        public string ClientUserAgent { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
