using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Services;

namespace Models
{
    public class Order(string? comments, int? idCurrentLocation)
    {
        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [ForeignKey("Client")]
        public int IdCliente { get; set; }
        [Required]
        public string Folio { get; set; } = string.Empty;
        [Required]
        public DateTimeOffset RegisterDate { get; set; } = DateTimeOffset.UtcNow;
        [Required]
        public DateTimeOffset ReceivedDate { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ReadyDate { get; set; }
        public DateTimeOffset? DeliveryDate { get; set; }

        public DateTimeOffset? CancelDate { get; set; }
        public int Adelanto { get; set; } = 0;
        public string? Comments { get; set; } = comments;
        public int? IdCurrentLocation { get; set; } = idCurrentLocation;
        public Location? CurrentLocation { get; set; }

        public Client? Client { get; set; }

        public List<OrderDetail>? OrderDetails { get; set; }
    }
}