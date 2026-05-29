using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Suggestion(int id, string name, string key, int price)
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = id;
        public string Name { get; set; } = name;
        public string? Description { get; set; }
        public string Key { get; set; } = key;
        public int Price { get; set; } = price;
    }
}
