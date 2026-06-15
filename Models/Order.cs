using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Services;

namespace Models
{
    public class Order(int idCliente, string? comments, int? idCurrentLocation, string folio)
    {
        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        public int IdCliente { get; set; } = idCliente;
        [Required]
        public string Folio { get; set; } = folio;
        [Required]
        public DateTime RegisterDate { get; set; } = DateTime.UtcNow;
        [Required]
        public DateTime ReceivedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReadyDate { get; set; }
        public DateTime? DeliveryDate { get; set; }

        public DateTime? CancelDate { get; set; }
        public int Adelanto { get; set; } = 0;
        public string? Comments { get; set; } = comments;
        public int? IdCurrentLocation { get; set; } = idCurrentLocation;
        public Location? CurrentLocation { get; set; }

        public Client? Client { get; set; }

        public List<OrderDetail>? OrderDetails { get; set; }
    }
}