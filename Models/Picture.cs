using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models;

public class Picture()
{
    [Required]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public required int Id { get; set; }
    [Required]
    public required string Rute { get; set; }
    [Required]
    [ForeignKey("Order")]
    public required int OrderId { get; set; }
    [ForeignKey("OrderDetail")]
    public int? OrderDetailId { get; set; }
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? DeletedAt { get; set; }

    public Order? Order { get; set; }

    public OrderDetail? OrderDetail { get; set; }
}