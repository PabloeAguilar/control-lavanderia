using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class OrderMovement(int orderId, int idLocation, DateTime date, int? quantity)
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        [Required]
        public int? Id { get; set; }
        public Order? Order { get; set; }
        [ForeignKey("Order")]
        public int? OrderId { get; set; } = orderId;
        [Required]
        [ForeignKey("Location")] 
        public int LocationId { get; set; } = idLocation;
        public Location? Location { get; set; }
        public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;
        public int? Quantity { get; set; } = quantity;

        public string? Comments { get; set; }

    }
}