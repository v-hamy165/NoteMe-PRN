using System;
using System.ComponentModel.DataAnnotations;

namespace NoteMe.Models
{
    public class AudioRecording
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(260)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string FileName { get; set; } = string.Empty;

        public int NoteId { get; set; }

        public Note? Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
