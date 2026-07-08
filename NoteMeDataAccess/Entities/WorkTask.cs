using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NoteMe.Models
{
    public enum WorkTaskPriority
    {
        Low = 0,
        Medium = 1,
        High = 2
    }

    public enum WorkTaskStatus
    {
        Todo = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3
    }

    public class WorkTask
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int? NoteId { get; set; }
        public Note? Note { get; set; }

        public int? MeetingSummaryId { get; set; }
        public MeetingSummary? MeetingSummary { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }
        public WorkTaskPriority Priority { get; set; } = WorkTaskPriority.Medium;
        public WorkTaskStatus Status { get; set; } = WorkTaskStatus.Todo;
        public DateTime? ReminderAt { get; set; }
        public bool ReminderSent { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        [NotMapped]
        public string PriorityDisplay => Priority switch
        {
            WorkTaskPriority.High => "Cao",
            WorkTaskPriority.Medium => "Trung bình",
            _ => "Thấp"
        };

        [NotMapped]
        public string StatusDisplay => Status switch
        {
            WorkTaskStatus.InProgress => "Đang làm",
            WorkTaskStatus.Completed => "Hoàn thành",
            WorkTaskStatus.Cancelled => "Đã hủy",
            _ => "Cần làm"
        };

        [NotMapped]
        public string SourceDisplay => Note?.Title ?? (MeetingSummaryId.HasValue ? "Tóm tắt AI" : "Tạo thủ công");
    }
}
