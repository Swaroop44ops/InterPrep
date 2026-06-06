using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Flashcard
    {
        public int Id { get; set; }

        [Required]
        public string Front { get; set; } = string.Empty;

        [Required]
        public string Back { get; set; } = string.Empty;

        [Required]
        public int TopicId { get; set; }

        [ForeignKey("TopicId")]
        public Topic? Topic { get; set; }

        public DateTime NextReviewDate { get; set; } = DateTime.UtcNow;

        public int IntervalDays { get; set; } = 1;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
