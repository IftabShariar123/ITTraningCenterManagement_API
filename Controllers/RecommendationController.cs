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

        

        [HttpGet("GetRecommendations")]
        public async Task<ActionResult<IEnumerable<object>>> GetRecommendations()
        {
            var data = await _context.Recommendations
                .Include(r => r.Trainee)
                    .ThenInclude(t => t.Registration)
                .Include(r => r.Batch)
                .Include(r => r.Instructor)
                    .ThenInclude(i => i.Employee)
                .Include(r => r.Assessment)
                .Include(r => r.Invoice)
                .Select(r => new
                {
                    r.RecommendationId,
                    TraineeName = r.Trainee != null && r.Trainee.Registration != null
                        ? r.Trainee.Registration.TraineeName
                        : "Unnamed Trainee",
                    InstructorName = r.Instructor != null && r.Instructor.Employee != null
                        ? r.Instructor.Employee.EmployeeName
                        : null,
                    BatchName = r.Batch != null ? r.Batch.BatchName : null,
                    r.RecommendationStatus,
                    r.RecommendationDate,
                    r.AssessmentId,
                    r.InvoiceId,
                    r.RecommendationText,
                    // add any other fields you want to expose
                })
                .ToListAsync();

            return Ok(data);
        }



        [HttpGet("GetRecommendation/{id}")]
        public async Task<ActionResult<Recommendation>> GetRecommendation(int id)
        {
            var recommendation = await _context.Recommendations
                .Include(r => r.Trainee)
                    .ThenInclude(t => t.Registration) 
                .Include(r => r.Instructor)
                    .ThenInclude(i => i.Employee)    
                .Include(r => r.Batch)                
                .Include(r => r.Assessment)           
                .Include(r => r.Invoice)              
                .FirstOrDefaultAsync(r => r.RecommendationId == id);

            if (recommendation == null)
            {
                return NotFound();
            }

            return recommendation;
        }

        // POST: api/Recommendation
        [HttpPost("InsertRecommendation")]
        public async Task<ActionResult<Recommendation>> CreateRecommendation(Recommendation recommendation)
        {
            _context.Recommendations.Add(recommendation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRecommendation), new { id = recommendation.RecommendationId }, recommendation);
        }

        // PUT: api/Recommendation/5
        [HttpPut("UpdateRecommendation/{id}")]
        public async Task<IActionResult> UpdateRecommendation(int id, Recommendation recommendation)
        {
            if (id != recommendation.RecommendationId)
                return BadRequest();

            _context.Entry(recommendation).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Recommendations.Any(e => e.RecommendationId == id))
                    return NotFound();
                else
                    throw;
            }

            return NoContent();
        }

        // DELETE: api/Recommendation/5
        [HttpDelete("DeleteRecommendation/{id}")]
        public async Task<IActionResult> DeleteRecommendation(int id)
        {
            var recommendation = await _context.Recommendations.FindAsync(id);
            if (recommendation == null)
            {
                return NotFound();
            }

            _context.Recommendations.Remove(recommendation);
            await _context.SaveChangesAsync();

            return NoContent();
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


        //[HttpGet("GetInvAssessbyTrainee/{traineeId}")]
        //public async Task<ActionResult<object>> GetTraineeDocuments(int traineeId)
        //{
        //    // Get the trainee with admission details
        //    var trainee = await _context.Trainees
        //        .Include(t => t.Admission)
        //        .Include(t => t.Registration)
        //        .FirstOrDefaultAsync(t => t.TraineeId == traineeId);

        //    if (trainee == null)
        //    {
        //        return NotFound("Trainee not found");
        //    }

        //    // Get all money receipts for this admission
        //    var moneyReceipts = await _context.MoneyReceipts
        //        .Where(mr => mr.AdmissionId == trainee.AdmissionId)
        //        .Include(mr => mr.Invoice)
        //        .ToListAsync();

        //    // Get all assessments for this trainee
        //    var assessments = await _context.Assessments
        //        .Where(a => a.TraineeId == traineeId)
        //        .ToListAsync();

        //    // Prepare invoice data from money receipts
        //    var invoices = moneyReceipts
        //        .Where(mr => mr.Invoice != null)
        //        .Select(mr => new
        //        {
        //            InvoiceId = mr.Invoice!.InvoiceId,
        //            InvoiceNo = mr.Invoice.InvoiceNo,
        //            MoneyReceiptNo = mr.MoneyReceiptNo,
        //            Category = mr.Category,
        //            Amount = mr.PaidAmount,
        //            Date = mr.Invoice.CreatingDate,
        //            IsInvoiceCreated = mr.IsInvoiceCreated
        //        })
        //        .ToList();

        //    // Prepare assessment data
        //    var assessmentData = assessments.Select(a => new
        //    {
        //        a.AssessmentId,
        //        a.AssessmentType,
        //        a.AssessmentDate,
        //        a.OverallScore,
        //        a.IsFinalized,
        //        a.TheoreticalScore,
        //        a.PracticalScore
        //    }).ToList();

        //    return Ok(new
        //    {
        //        Invoices = invoices,
        //        Assessments = assessmentData,
        //        TraineeName = trainee.Registration?.TraineeName // Get name from Registration via navigation
        //    });
        //}

        [HttpGet("GetInvAssessbyTrainee/{traineeId}")]
        public async Task<ActionResult<object>> GetInvAssessbyTrainee(int traineeId)
        {
            // ১. Trainee থেকে AdmissionId বের করা
            var trainee = await _context.Trainees
                .FirstOrDefaultAsync(t => t.TraineeId == traineeId);

            if (trainee == null)
            {
                return NotFound("Trainee not found");
            }

            // ২. AdmissionId দিয়ে MoneyReceipts নিয়ে আসা + Invoice navigation eager load
            var moneyReceipts = await _context.MoneyReceipts
                .Where(mr => mr.AdmissionId == trainee.AdmissionId)
                .Include(mr => mr.Invoice)
                .ToListAsync();

            // ৩. Invoice গুলো distinct করা (InvoiceNo ভিত্তিতে)
            var distinctInvoices = moneyReceipts
                .Where(mr => mr.Invoice != null) // Invoice নাল নয়
                .GroupBy(mr => mr.Invoice!.InvoiceNo)
                .Select(g => g.First().Invoice)  // প্রথম Invoice নিয়ে আসা
                .ToList();

            // ৪. Assessment গুলো TraineeId দিয়ে filter করা
            var assessments = await _context.Assessments
                .Where(a => a.TraineeId == traineeId)
                .ToListAsync();

            return Ok(new
            {
                invoices = distinctInvoices.Select(inv => new
                {
                    inv!.InvoiceId,
                    inv.InvoiceNo,
                    inv.CreatingDate,
                    inv.InvoiceCategory,
                    inv.MoneyReceiptNo
                }),
                assessments = assessments.Select(a => new
                {
                    a.AssessmentId,
                    a.AssessmentType,
                    a.AssessmentDate,
                    a.OverallScore,
                    a.IsFinalized,
                    a.TheoreticalScore,
                    a.PracticalScore
                }),
                traineeName = trainee.Registration?.TraineeName // Registration থেকে নাম
            });
        }

    }

}