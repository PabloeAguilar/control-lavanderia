using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class OrderMovement
    {
        public OrderMovement()
        {
        }

        public OrderMovement(int orderId, int locationId, DateTime date, int? quantity)
        {
            OrderId = orderId;
            LocationId = locationId;
            Date = new DateTimeOffset(date, TimeSpan.Zero);
            Quantity = quantity;
        }

        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [Required]
        public int? Id { get; set; }
        public Order? Order { get; set; }
        [ForeignKey("Order")]
        public int? OrderId { get; set; }
        [Required]
        [ForeignKey("Location")]
        public int LocationId { get; set; }
        public Location? Location { get; set; }
        public DateTimeOffset Date { get; set; } = DateTimeOffset.UtcNow;
        public int? Quantity { get; set; }

        public string? Comments { get; set; }

    }
}