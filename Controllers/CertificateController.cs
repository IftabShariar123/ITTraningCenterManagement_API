using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingCenter_Api.Data;
using TrainingCenter_Api.Models;

namespace TrainingCenter_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificateController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CertificateController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Certificate
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Certificate>>> GetCertificates()
        {
            return await _context.Certificates
                .Include(c => c.Trainee)
                .Include(c => c.Registration)
                .Include(c => c.Batch)
                .Include(c => c.Course)
                .Include(c => c.Recommendation)
                .ToListAsync();
        }

        // POST: api/Certificate
        [HttpPost("Create")]
        public async Task<IActionResult> CreateCertificate([FromBody] Certificate certificate)
        {
            var recommendation = await _context.Recommendations
                .Include(r => r.Trainee)
                .Include(r => r.Instructor)
                .FirstOrDefaultAsync(r => r.RecommendationId == certificate.RecommendationId);

            if (recommendation == null)
            {
                return BadRequest("Recommendation not found.");
            }

            if (recommendation.Status != RecommendationStatus.Approved)
            {
                var trainee = await _context.Trainees
                    .Include(t => t.Registration)
                    .FirstOrDefaultAsync(t => t.TraineeId == certificate.TraineeId);

                var traineeName = trainee?.Registration?.TraineeName ?? "Unknown";
                var traineeCode = trainee?.TraineeIDNo ?? "N/A";

                return BadRequest($"❌ Trainee '{traineeName}' (ID: {traineeCode}) is not approved for certificate issuance.");
            }

            // Auto-generate certificate number
            certificate.CertificateNumber = await GenerateNextCertificateNumber();
            certificate.IssueDate = DateTime.Now;

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            return Ok(certificate);
        }

        // Helper: Generate next certificate number
        private async Task<string> GenerateNextCertificateNumber()
        {
            var lastCert = await _context.Certificates
                .OrderByDescending(c => c.CertificateId)
                .Select(c => c.CertificateNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (!string.IsNullOrEmpty(lastCert) && lastCert.StartsWith("CERT-"))
            {
                var numeric = lastCert.Substring(5);
                if (int.TryParse(numeric, out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }

            return $"CERT-{nextNumber:D6}";
        }

        // 🔄 Dropdown: Batch → Course + Trainees
        [HttpGet("LoadCourseAndTraineesByBatch/{batchId}")]
        public async Task<IActionResult> LoadCourseAndTraineesByBatch(int batchId)
        {
            var batch = await _context.Batches
                .Include(b => b.Course)
                .FirstOrDefaultAsync(b => b.BatchId == batchId);

            if (batch == null) return NotFound("Batch not found.");

            var trainees = await _context.Trainees
                .Include(t => t.Registration)
                .Where(t => t.BatchId == batchId)
                .Select(t => new
                {
                    t.TraineeId,
                    NameWithId = t.Registration.TraineeName + " (" + t.TraineeIDNo + ")"
                })
                .ToListAsync();

            return Ok(new
            {
                courseId = batch.CourseId,
                courseName = batch.Course.CourseName,
                trainees
            });
        }

        // 🔄 Dropdown: Trainee → Registration ID + Recommendation Status
        [HttpGet("LoadTraineeInfo/{traineeId}")]
        public async Task<IActionResult> LoadTraineeInfo(int traineeId)
        {
            var trainee = await _context.Trainees
                .Include(t => t.Registration)
                .FirstOrDefaultAsync(t => t.TraineeId == traineeId);

            if (trainee == null) return NotFound("Trainee not found.");

            var recommendation = await _context.Recommendations
                .FirstOrDefaultAsync(r => r.TraineeId == traineeId && r.Status == RecommendationStatus.Approved);

            return Ok(new
            {
                registrationId = trainee.RegistrationId,
                registrationNo = trainee.Registration?.RegistrationNo ?? "N/A",
                isRecommended = recommendation != null,
                recommendationId = recommendation?.RecommendationId
            });
        }

        // PUT: api/Certificate/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCertificate(int id, Certificate certificate)
        {
            if (id != certificate.CertificateId)
                return BadRequest();

            _context.Entry(certificate).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Certificate/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            var certificate = await _context.Certificates.FindAsync(id);
            if (certificate == null)
                return NotFound();

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Certificate/ByTrainee/1
        [HttpGet("ByTrainee/{traineeId}")]
        public async Task<IActionResult> GetByTrainee(int traineeId)
        {
            var certificates = await _context.Certificates
                .Where(c => c.TraineeId == traineeId)
                .Include(c => c.Trainee)
                .Include(c => c.Registration)
                .Include(c => c.Course)
                .ToListAsync();

            return Ok(certificates);
        }
    }
}
