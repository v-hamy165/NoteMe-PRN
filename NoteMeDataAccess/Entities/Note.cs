using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NoteMe.Models
{
    public class Note
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Category { get; set; } = string.Empty;

        public int UserId { get; set; }

        public User? User { get; set; }

        public ICollection<AudioRecording> AudioRecordings { get; set; } =
            new List<AudioRecording>();

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
