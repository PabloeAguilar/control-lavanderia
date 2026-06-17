using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class OrderDetail(int id, int orderId, string type, int price, string? description, decimal quantity)
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = id;
        [Required]
        public int OrderId { get; set; } = orderId;
        [Required]
        public string Type { get; set; } = type;
        [Required]
        public int Price { get; set; } = price;
        public string? Description { get; set; } = description;
        public decimal Quantity { get; set; } = quantity;
        public int? Total { get; set; } = (int)Math.Floor(price * quantity);

        public DateTimeOffset RegisterDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? UpdateDate { get; set; }
    }

}