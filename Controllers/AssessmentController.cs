using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingCenter_Api.Data;
using TrainingCenter_Api.Models;

namespace TrainingCenter_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssessmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssessmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("GetAssessments")]
        public async Task<ActionResult<IEnumerable<Assessment>>> GetAllAssessments()
        {
            var assessments = await _context.Assessments
                .Include(a => a.Trainee)
                  .ThenInclude(t => t.Registration)
                .Include(a => a.Batch)
                .Include(a => a.Instructor)
                .Include(a => a.Recommendations)
                .ToListAsync();

            return Ok(assessments);
        }

        [HttpGet("GetAssessment/{id}")]
        public async Task<ActionResult<Assessment>> GetAssessment(int id)
        {
            var assessment = await _context.Assessments
                .Include(a => a.Trainee)
                    .ThenInclude(t => t.Registration) 
                .Include(a => a.Batch)
                .Include(a => a.Instructor)
                    .ThenInclude(i => i.Employee) 
                .Include(a => a.Recommendations)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
                return NotFound();

            return Ok(assessment);
        }


        [HttpPost("InsertAssessment")]
        public async Task<IActionResult> CreateAssessment([FromBody] Assessment assessment)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Calculate AttendancePercentage
                if (assessment.TotalDays > 0)
                {
                    assessment.AttendancePercentage = assessment.DaysPresent * 100m / assessment.TotalDays;
                }

                // Calculate OverallScore
                assessment.OverallScore = (assessment.TheoreticalScore + assessment.PracticalScore) / 2m;

                // Save the main Assessment
                _context.Assessments.Add(assessment);
                await _context.SaveChangesAsync();

                // Save Recommendations
                if (assessment.Recommendations != null && assessment.Recommendations.Any())
                {
                    foreach (var rec in assessment.Recommendations)
                    {
                        rec.AssessmentId = assessment.AssessmentId;
                        _context.Recommendations.Add(rec);
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                return Ok(new { message = "Assessment created successfully", assessmentId = assessment.AssessmentId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred", error = ex.Message });
            }
        }

        [HttpPut("UpdateAssessment/{id}")]
        public async Task<IActionResult> UpdateAssessment(int id, [FromBody] Assessment updatedAssessment)
        {
            if (id != updatedAssessment.AssessmentId)
                return BadRequest("Assessment ID mismatch");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var existing = await _context.Assessments
                    .Include(a => a.Recommendations)
                    .FirstOrDefaultAsync(a => a.AssessmentId == id);

                if (existing == null)
                    return NotFound();

                // Update scalar fields
                _context.Entry(existing).CurrentValues.SetValues(updatedAssessment);

                // Recalculate AttendancePercentage
                if (updatedAssessment.TotalDays > 0)
                {
                    existing.AttendancePercentage = updatedAssessment.DaysPresent * 100m / updatedAssessment.TotalDays;
                }

                // Recalculate OverallScore
                existing.OverallScore = (updatedAssessment.TheoreticalScore + updatedAssessment.PracticalScore) / 2m;

                // Update Recommendations
                _context.Recommendations.RemoveRange(existing.Recommendations);

                if (updatedAssessment.Recommendations != null && updatedAssessment.Recommendations.Any())
                {
                    foreach (var rec in updatedAssessment.Recommendations)
                    {
                        rec.AssessmentId = id;
                        _context.Recommendations.Add(rec);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Assessment updated successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Update failed", error = ex.Message });
            }
        }


        // DELETE: api/Assessment/{id}
        [HttpDelete("DeleteAssessment/{id}")]
        public async Task<IActionResult> DeleteAssessment(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var assessment = await _context.Assessments
                    .Include(a => a.Recommendations)
                    .FirstOrDefaultAsync(a => a.AssessmentId == id);

                if (assessment == null)
                    return NotFound();

                // Remove related recommendations first
                _context.Recommendations.RemoveRange(assessment.Recommendations);
                _context.Assessments.Remove(assessment);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Assessment deleted successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Delete failed", error = ex.Message });
            }
        }         



        [HttpGet("GetInsTraiByBatch/{batchId}")]
        public async Task<ActionResult<object>> GetBatchWithDetails(int batchId)
        {
            // First get batch with instructor info
            var batch = await _context.Batches
                .Include(b => b.Instructor)
                    .ThenInclude(i => i.Employee)
                .FirstOrDefaultAsync(b => b.BatchId == batchId);

            if (batch == null)
            {
                return NotFound();
            }

            // Then get all trainees for this batch
            var trainees = await _context.Trainees
                .Where(t => t.BatchId == batchId)
                .Include(t => t.Registration)
                .ToListAsync();

            var result = new
            {
                Instructor = batch.Instructor != null ? new
                {
                    InstructorId = batch.Instructor.InstructorId,
                    InstructorName = batch.Instructor.Employee?.EmployeeName
                } : null,

                Trainees = trainees.Select(t => new
                {
                    TraineeId = t.TraineeId,
                    TraineeName = t.Registration?.TraineeName
                }).ToList()
            };

            return Ok(result);
        }


    }
}