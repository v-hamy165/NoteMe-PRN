using System;
using System.ComponentModel.DataAnnotations;

namespace NoteMe.Models
{
    public class MeetingSummary
    {
        [Key]
        public int Id { get; set; }

        public int NoteId { get; set; }

        public Note? Note { get; set; }

        [MaxLength(120)]
        public string AudioFileName { get; set; } = string.Empty;

        public string Transcript { get; set; } = string.Empty;

        [Required]
        public string MainContent { get; set; } = string.Empty;

        public string CompletedSteps { get; set; } = string.Empty;

        public string NextSteps { get; set; } = string.Empty;

        [MaxLength(50)]
        public string ModelUsed { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
