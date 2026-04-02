using KVYS.Education.Domain.Entities;
using KVYS.Education.Domain.Enums;
using KVYS.Education.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KVYS.Api.Controllers;

[ApiController]
[Route("api/v1/courses/{courseId}/instances/{instanceId}/[controller]")]
[Authorize]
public class ExamsController : ControllerBase
{
    private readonly EducationDbContext _context;
    private readonly ILogger<ExamsController> _logger;

    public ExamsController(EducationDbContext context, ILogger<ExamsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(Guid courseId, Guid instanceId)
    {
        var instance = await _context.CourseInstances
            .Include(i => i.Exams)
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CourseId == courseId);

        if (instance == null)
            return NotFound();

        var exams = instance.Exams
            .OrderBy(e => e.Date)
            .Select(e => new
            {
                e.Id,
                e.Name,
                e.Type,
                e.Date,
                e.Weight,
                e.TotalPoints
            });

        return Ok(exams);
    }

    [HttpGet("{examId}")]
    public async Task<IActionResult> GetById(Guid courseId, Guid instanceId, Guid examId)
    {
        var exam = await _context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.LearningOutcomeMappings)
                    .ThenInclude(m => m.LearningOutcome)
            .FirstOrDefaultAsync(e => e.Id == examId && e.CourseInstanceId == instanceId);

        if (exam == null)
            return NotFound();

        return Ok(new
        {
            exam.Id,
            exam.Name,
            exam.Type,
            exam.Date,
            exam.Weight,
            exam.TotalPoints,
            Questions = exam.Questions.OrderBy(q => q.QuestionNumber).Select(q => new
            {
                q.Id,
                q.QuestionNumber,
                q.Points,
                q.Description,
                LearningOutcomes = q.LearningOutcomeMappings.Select(m => new
                {
                    m.LearningOutcomeId,
                    m.LearningOutcome.Code,
                    m.LearningOutcome.Description
                })
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(Guid courseId, Guid instanceId, [FromBody] CreateExamRequest request)
    {
        var instance = await _context.CourseInstances
            .FirstOrDefaultAsync(i => i.Id == instanceId && i.CourseId == courseId);

        if (instance == null)
            return NotFound();

        var exam = new Exam(instanceId, request.Name, request.Type, request.Weight, request.Date);
        _context.Exams.Add(exam);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created exam: {Name} for instance {InstanceId}", request.Name, instanceId);
        return CreatedAtAction(nameof(GetById), new { courseId, instanceId, examId = exam.Id }, new { exam.Id });
    }

    [HttpPut("{examId}")]
    public async Task<IActionResult> Update(Guid courseId, Guid instanceId, Guid examId, [FromBody] UpdateExamRequest request)
    {
        var exam = await _context.Exams
            .FirstOrDefaultAsync(e => e.Id == examId && e.CourseInstanceId == instanceId);

        if (exam == null)
            return NotFound();

        exam.Update(request.Name, request.Type, request.Weight, request.Date);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{examId}")]
    public async Task<IActionResult> Delete(Guid courseId, Guid instanceId, Guid examId)
    {
        var exam = await _context.Exams
            .FirstOrDefaultAsync(e => e.Id == examId && e.CourseInstanceId == instanceId);

        if (exam == null)
            return NotFound();

        _context.Exams.Remove(exam);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Questions
    [HttpPost("{examId}/questions")]
    public async Task<IActionResult> AddQuestion(Guid courseId, Guid instanceId, Guid examId, [FromBody] CreateQuestionRequest request)
    {
        var exam = await _context.Exams
            .Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == examId && e.CourseInstanceId == instanceId);

        if (exam == null)
            return NotFound();

        var questionNumber = exam.Questions.Count + 1;
        var question = new ExamQuestion(examId, request.QuestionNumber ?? questionNumber, request.Points, request.Description);
        _context.ExamQuestions.Add(question);
        await _context.SaveChangesAsync();

        return Created("", new { question.Id });
    }

    [HttpPut("{examId}/questions/{questionId}")]
    public async Task<IActionResult> UpdateQuestion(Guid courseId, Guid instanceId, Guid examId, Guid questionId, [FromBody] UpdateQuestionRequest request)
    {
        var question = await _context.ExamQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == examId);

        if (question == null)
            return NotFound();

        question.Update(request.Points, request.Description);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{examId}/questions/{questionId}")]
    public async Task<IActionResult> DeleteQuestion(Guid courseId, Guid instanceId, Guid examId, Guid questionId)
    {
        var question = await _context.ExamQuestions
            .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == examId);

        if (question == null)
            return NotFound();

        _context.ExamQuestions.Remove(question);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Question-LO Mappings
    [HttpPost("{examId}/questions/{questionId}/learning-outcomes")]
    public async Task<IActionResult> AddQuestionLOMapping(Guid courseId, Guid instanceId, Guid examId, Guid questionId, [FromBody] CreateQuestionLOMappingRequest request)
    {
        var question = await _context.ExamQuestions
            .Include(q => q.LearningOutcomeMappings)
            .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == examId);

        if (question == null)
            return NotFound();

        var lo = await _context.LearningOutcomes
            .FirstOrDefaultAsync(lo => lo.Id == request.LearningOutcomeId && lo.CourseInstanceId == instanceId);

        if (lo == null)
            return BadRequest(new { error = "Learning outcome not found or doesn't belong to this instance" });

        if (question.LearningOutcomeMappings.Any(m => m.LearningOutcomeId == request.LearningOutcomeId))
            return BadRequest(new { error = "Mapping already exists" });

        var mapping = new QuestionLearningOutcomeMapping(questionId, request.LearningOutcomeId);
        _context.QuestionLearningOutcomeMappings.Add(mapping);
        await _context.SaveChangesAsync();

        return Created("", new { mapping.Id });
    }

    [HttpDelete("{examId}/questions/{questionId}/learning-outcomes/{loId}")]
    public async Task<IActionResult> DeleteQuestionLOMapping(Guid courseId, Guid instanceId, Guid examId, Guid questionId, Guid loId)
    {
        var mapping = await _context.QuestionLearningOutcomeMappings
            .FirstOrDefaultAsync(m => m.QuestionId == questionId && m.LearningOutcomeId == loId);

        if (mapping == null)
            return NotFound();

        _context.QuestionLearningOutcomeMappings.Remove(mapping);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // Student Scores
    [HttpPost("{examId}/questions/{questionId}/scores")]
    public async Task<IActionResult> AddScore(Guid courseId, Guid instanceId, Guid examId, Guid questionId, [FromBody] CreateScoreRequest request)
    {
        var question = await _context.ExamQuestions
            .Include(q => q.Scores)
            .FirstOrDefaultAsync(q => q.Id == questionId && q.ExamId == examId);

        if (question == null)
            return NotFound();

        if (request.Score < 0 || request.Score > question.Points)
            return BadRequest(new { error = $"Score must be between 0 and {question.Points}" });

        var existingScore = question.Scores.FirstOrDefault(s => s.StudentId == request.StudentId);
        if (existingScore != null)
        {
            existingScore.UpdateScore(request.Score);
        }
        else
        {
            var score = new StudentScore(questionId, request.StudentId, request.Score);
            _context.StudentScores.Add(score);
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{examId}/scores/bulk")]
    public async Task<IActionResult> AddBulkScores(Guid courseId, Guid instanceId, Guid examId, [FromBody] BulkScoreRequest request)
    {
        var exam = await _context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.Scores)
            .FirstOrDefaultAsync(e => e.Id == examId && e.CourseInstanceId == instanceId);

        if (exam == null)
            return NotFound();

        foreach (var scoreEntry in request.Scores)
        {
            var question = exam.Questions.FirstOrDefault(q => q.Id == scoreEntry.QuestionId);
            if (question == null)
                continue;

            if (scoreEntry.Score < 0 || scoreEntry.Score > question.Points)
                continue;

            var existingScore = question.Scores.FirstOrDefault(s => s.StudentId == scoreEntry.StudentId);
            if (existingScore != null)
            {
                existingScore.UpdateScore(scoreEntry.Score);
            }
            else
            {
                var score = new StudentScore(scoreEntry.QuestionId, scoreEntry.StudentId, scoreEntry.Score);
                _context.StudentScores.Add(score);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Bulk score entry completed for exam {ExamId}: {Count} scores", examId, request.Scores.Count);

        return Ok(new { message = $"Processed {request.Scores.Count} scores" });
    }

    // Exam Analysis - LO Achievement
    [HttpGet("{examId}/analysis")]
    public async Task<IActionResult> GetExamAnalysis(Guid courseId, Guid instanceId, Guid examId)
    {
        var exam = await _context.Exams
            .Include(e => e.Questions)
                .ThenInclude(q => q.LearningOutcomeMappings)
                    .ThenInclude(m => m.LearningOutcome)
            .Include(e => e.Questions)
                .ThenInclude(q => q.Scores)
            .FirstOrDefaultAsync(e => e.Id == examId && e.CourseInstanceId == instanceId);

        if (exam == null)
            return NotFound();

        var students = exam.Questions
            .SelectMany(q => q.Scores)
            .Select(s => s.StudentId)
            .Distinct()
            .ToList();

        // Calculate LO achievement per student
        var loGroups = exam.Questions
            .SelectMany(q => q.LearningOutcomeMappings.Select(m => new
            {
                m.LearningOutcome.Id,
                m.LearningOutcome.Code,
                m.LearningOutcome.Description,
                Question = q
            }))
            .GroupBy(x => new { x.Id, x.Code, x.Description })
            .Select(g => new
            {
                LearningOutcomeId = g.Key.Id,
                Code = g.Key.Code,
                Description = g.Key.Description,
                MaxPoints = g.Sum(x => x.Question.Points),
                Questions = g.Select(x => x.Question.Id).Distinct().ToList()
            })
            .ToList();

        var loAchievements = loGroups.Select(lo =>
        {
            var totalScores = students.Select(studentId =>
            {
                var studentPoints = lo.Questions
                    .Select(qId => exam.Questions.First(q => q.Id == qId))
                    .SelectMany(q => q.Scores.Where(s => s.StudentId == studentId))
                    .Sum(s => s.Score);
                return studentPoints;
            }).ToList();

            var avgScore = totalScores.Any() ? totalScores.Average() : 0;
            var achievementRate = lo.MaxPoints > 0 ? (avgScore / lo.MaxPoints) * 100 : 0;

            return new
            {
                lo.LearningOutcomeId,
                lo.Code,
                lo.Description,
                lo.MaxPoints,
                AverageScore = Math.Round(avgScore, 2),
                AchievementRate = Math.Round(achievementRate, 2)
            };
        });

        return Ok(new
        {
            ExamId = exam.Id,
            ExamName = exam.Name,
            TotalStudents = students.Count,
            TotalPoints = exam.TotalPoints,
            LearningOutcomeAnalysis = loAchievements
        });
    }
}

// Request DTOs
public record CreateExamRequest(
    string Name,
    ExamType Type,
    decimal Weight,
    DateOnly? Date = null
);

public record UpdateExamRequest(
    string Name,
    ExamType Type,
    decimal Weight,
    DateOnly? Date = null
);

public record CreateQuestionRequest(
    decimal Points,
    string? Description = null,
    int? QuestionNumber = null
);

public record UpdateQuestionRequest(
    decimal Points,
    string? Description = null
);

public record CreateQuestionLOMappingRequest(Guid LearningOutcomeId);

public record CreateScoreRequest(
    string StudentId,
    decimal Score
);

public record BulkScoreRequest(List<ScoreEntry> Scores);

public record ScoreEntry(
    Guid QuestionId,
    string StudentId,
    decimal Score
);
