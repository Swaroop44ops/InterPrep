using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class UserQuestionStatus
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [ForeignKey("QuestionId")]
        public Question? Question { get; set; }

        [Required]
        public string Status { get; set; } = "Unseen"; // "Unseen", "Attempted", "Confident"

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
