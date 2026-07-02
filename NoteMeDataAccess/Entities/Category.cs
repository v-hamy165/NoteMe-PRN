using System.ComponentModel.DataAnnotations;

namespace NoteMe.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}