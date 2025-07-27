using System.ComponentModel.DataAnnotations;

namespace TrainingCenter_Api.Models
{
    public class Certificate
    {
        [Key]
        public int CertificateId { get; set; }

        [Required]
        public int TraineeId { get; set; }
        public virtual Trainee? Trainee { get; set; }

        [Required]
        public int RegistrationId { get; set; }
        public virtual Registration? Registration { get; set; }

        [Required]
        public int BatchId { get; set; }
        public virtual Batch? Batch { get; set; }

        [Required]
        public int CourseId { get; set; }
        public virtual Course? Course { get; set; }

        [Required]
        public int RecommendationId { get; set; }
        public virtual Recommendation? Recommendation { get; set; }

        public DateTime IssueDate { get; set; } = DateTime.Now;

        [MaxLength(100)]
        public string? CertificateNumber { get; set; } // ইউনিক ও ম্যানুয়ালি বা অটো জেনারেট করা যাবে
    }
}
