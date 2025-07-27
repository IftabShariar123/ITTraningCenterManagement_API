using System.ComponentModel.DataAnnotations;

namespace TrainingCenter_Api.Models
{
    public enum RecommendationStatus
    {
        Pending,
        Approved,
        Rejected
    }
    public class Recommendation
    {
        [Key]
        public int RecommendationId { get; set; }

        [Required]
        public int TraineeId { get; set; }
        public Trainee? Trainee { get; set; }

        [Required]
        public int InstructorId { get; set; }
        public Instructor? Instructor { get; set; }

        [Required]
        public int BatchId { get; set; }
        public Batch? Batch { get; set; }

        [Required]
        public int AssessmentId { get; set; } // যে Assessment Finalized হয়েছে
        public Assessment? Assessment { get; set; }

        [Required]
        public int InvoiceId { get; set; } // ক্লিয়ার ইনভয়েস
        public Invoice? Invoice { get; set; }

        [Required]
        [MaxLength(2000)]
        public string RecommendationText { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime RecommendationDate { get; set; }

        public RecommendationStatus Status { get; set; } = RecommendationStatus.Pending;
    }
}
