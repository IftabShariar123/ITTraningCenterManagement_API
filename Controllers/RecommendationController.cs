using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingCenter_Api.Data;
using TrainingCenter_Api.Models;

namespace TrainingCenter_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RecommendationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ GET: api/Recommendation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Recommendation>>> GetRecommendations()
        {
            return await _context.Recommendations
                .Include(r => r.Trainee)
                .Include(r => r.Instructor)
                .Include(r => r.Batch)
                .Include(r => r.Assessment)
                .Include(r => r.Invoice)
                .ToListAsync();
        }

        // ✅ GET: api/Recommendation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Recommendation>> GetRecommendation(int id)
        {
            var recommendation = await _context.Recommendations
                .Include(r => r.Trainee)
                .Include(r => r.Instructor)
                .Include(r => r.Batch)
                .Include(r => r.Assessment)
                .Include(r => r.Invoice)
                .FirstOrDefaultAsync(r => r.RecommendationId == id);

            if (recommendation == null)
                return NotFound();

            return Ok(recommendation);
        }

        // ✅ POST: api/Recommendation
        [HttpPost]
        public async Task<IActionResult> CreateRecommendation([FromBody] Recommendation recommendation)
        {
            // Validation: Assessment must be finalized
            var assessment = await _context.Assessments
                .FirstOrDefaultAsync(a => a.AssessmentId == recommendation.AssessmentId);
            if (assessment == null || !assessment.IsFinalized)
                return BadRequest("Assessment must be finalized before recommendation.");

            // Validation: Invoice must exist and be fully paid
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.InvoiceId == recommendation.InvoiceId);
            if (invoice == null)
                return BadRequest("Invoice not found.");

            var totalDue = await _context.MoneyReceipts
                .Where(m => m.InvoiceId == invoice.InvoiceId)
                .SumAsync(m => m.DueAmount);
            if (totalDue > 0)
                return BadRequest("Invoice has unpaid dues.");

            // Assign RecommendationDate and default status if not passed
            recommendation.RecommendationDate = DateTime.Now;

            if (!Enum.IsDefined(typeof(RecommendationStatus), recommendation.Status))
                recommendation.Status = RecommendationStatus.Pending;

            _context.Recommendations.Add(recommendation);
            await _context.SaveChangesAsync();

            return Ok(recommendation);
        }

        // ✅ PUT: api/Recommendation/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRecommendation(int id, [FromBody] Recommendation recommendation)
        {
            if (id != recommendation.RecommendationId)
                return BadRequest("Mismatched ID");

            var existing = await _context.Recommendations.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Update fields manually to prevent overposting
            existing.TraineeId = recommendation.TraineeId;
            existing.InstructorId = recommendation.InstructorId;
            existing.BatchId = recommendation.BatchId;
            existing.AssessmentId = recommendation.AssessmentId;
            existing.InvoiceId = recommendation.InvoiceId;
            existing.RecommendationText = recommendation.RecommendationText;

            // Allow status update (Pending, Approved, Rejected)
            if (Enum.IsDefined(typeof(RecommendationStatus), recommendation.Status))
                existing.Status = recommendation.Status;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // ✅ DELETE: api/Recommendation/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecommendation(int id)
        {
            var recommendation = await _context.Recommendations.FindAsync(id);
            if (recommendation == null)
                return NotFound();

            _context.Recommendations.Remove(recommendation);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
