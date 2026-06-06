using System;
using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class StudySession
    {
        public int Id { get; set; }

        [Required]
        public int DurationSeconds { get; set; }

        public int NotesReviewedCount { get; set; }

        public int CardsReviewedCount { get; set; }

        public int QuestionsAttemptedCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
