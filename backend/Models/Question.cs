using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public string Text { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;

        [Required]
        public int TopicId { get; set; }

        [ForeignKey("TopicId")]
        public Topic? Topic { get; set; }

        [Required]
        public string Difficulty { get; set; } = "Medium"; // "Easy", "Medium", "Hard"

        [Required]
        public string Status { get; set; } = "Unseen"; // "Unseen", "Attempted", "Confident"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
