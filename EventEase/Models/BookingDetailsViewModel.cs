namespace EventEase.Models
{
    public class BookingDetailsViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference => $"BK{BookingId:D6}";
        public DateTime BookingDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? CustomerPhone { get; set; }
        public string? Notes { get; set; }

        // Venue info
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;
        public string VenueLocation { get; set; } = string.Empty;
        public int VenueCapacity { get; set; }
        public string? VenueImageUrl { get; set; }

        // Event info
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? EventDescription { get; set; }
        public DateTime EventStartDate { get; set; }
        public DateTime EventEndDate { get; set; }
        public string? EventImageUrl { get; set; }
    }
}
