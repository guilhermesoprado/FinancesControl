using System.Security.Claims;
using FinanceManager.Api.Contracts.Requests.ScheduledEntries;
using FinanceManager.Api.Contracts.Responses.ScheduledEntries;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.ScheduledEntries;
using FinanceManager.Application.ScheduledEntries.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/scheduled-entries")]
public sealed class ScheduledEntriesController : ControllerBase
{
    private readonly IScheduledEntryService _scheduledEntryService;

    public ScheduledEntriesController(IScheduledEntryService scheduledEntryService)
    {
        _scheduledEntryService = scheduledEntryService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ScheduledEntryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledEntryResponse>> Create(
        [FromBody] CreateScheduledEntryRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledEntry = await _scheduledEntryService.CreateAsync(
            new CreateScheduledEntryInput(
                GetAuthenticatedUserId(),
                request.FinancialAccountId,
                request.TransactionCategoryId,
                ParsePlanningMode(request.PlanningMode),
                ParseNullableRecurrenceFrequency(request.RecurrenceFrequency),
                request.Amount,
                request.Description,
                request.StartDate,
                request.EndDate),
            cancellationToken);

        return Ok(MapResponse(scheduledEntry));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ScheduledEntryOccurrenceResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScheduledEntryOccurrenceResponse>>> Get(
        [FromQuery] string? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var scheduledEntries = await _scheduledEntryService.GetByUserAsync(
            new GetScheduledEntriesInput(
                GetAuthenticatedUserId(),
                ParseNullableStatus(status),
                from,
                to),
            cancellationToken);

        return Ok(scheduledEntries.Select(MapOccurrenceResponse).ToList());
    }

    [HttpPut("{scheduledEntryId:guid}")]
    [ProducesResponseType(typeof(ScheduledEntryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledEntryResponse>> Update(
        Guid scheduledEntryId,
        [FromBody] UpdateScheduledEntryRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledEntry = await _scheduledEntryService.UpdateAsync(
            new UpdateScheduledEntryInput(
                GetAuthenticatedUserId(),
                scheduledEntryId,
                request.FinancialAccountId,
                request.TransactionCategoryId,
                ParsePlanningMode(request.PlanningMode),
                ParseNullableRecurrenceFrequency(request.RecurrenceFrequency),
                request.Amount,
                request.Description,
                request.StartDate,
                request.EndDate),
            cancellationToken);

        return Ok(MapResponse(scheduledEntry));
    }

    [HttpPost("{scheduledEntryId:guid}/complete")]
    [ProducesResponseType(typeof(ScheduledEntryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledEntryResponse>> Complete(
        Guid scheduledEntryId,
        [FromBody] ScheduledEntryOccurrenceActionRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledEntry = await _scheduledEntryService.CompleteAsync(
            new ApplyScheduledEntryOccurrenceActionInput(
                GetAuthenticatedUserId(),
                scheduledEntryId,
                request.OccurrenceDate),
            cancellationToken);
        return Ok(MapResponse(scheduledEntry));
    }

    [HttpPost("{scheduledEntryId:guid}/undo-complete")]
    [ProducesResponseType(typeof(ScheduledEntryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledEntryResponse>> UndoComplete(
        Guid scheduledEntryId,
        [FromBody] ScheduledEntryOccurrenceActionRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledEntry = await _scheduledEntryService.UndoCompleteAsync(
            new ApplyScheduledEntryOccurrenceActionInput(
                GetAuthenticatedUserId(),
                scheduledEntryId,
                request.OccurrenceDate),
            cancellationToken);
        return Ok(MapResponse(scheduledEntry));
    }

    [HttpPost("{scheduledEntryId:guid}/skip")]
    [ProducesResponseType(typeof(ScheduledEntryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledEntryResponse>> Skip(
        Guid scheduledEntryId,
        [FromBody] ScheduledEntryOccurrenceActionRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledEntry = await _scheduledEntryService.SkipAsync(
            new ApplyScheduledEntryOccurrenceActionInput(
                GetAuthenticatedUserId(),
                scheduledEntryId,
                request.OccurrenceDate),
            cancellationToken);
        return Ok(MapResponse(scheduledEntry));
    }

    [HttpPost("{scheduledEntryId:guid}/cancel")]
    [ProducesResponseType(typeof(ScheduledEntryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ScheduledEntryResponse>> Cancel(
        Guid scheduledEntryId,
        [FromBody] ScheduledEntryOccurrenceActionRequest request,
        CancellationToken cancellationToken)
    {
        var scheduledEntry = await _scheduledEntryService.CancelAsync(
            new ApplyScheduledEntryOccurrenceActionInput(
                GetAuthenticatedUserId(),
                scheduledEntryId,
                request.OccurrenceDate),
            cancellationToken);
        return Ok(MapResponse(scheduledEntry));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(ClaimTypes.Sid)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            throw new AppUnauthorizedException("Usuario autenticado nao encontrado.");
        }

        return userId;
    }

    private static ScheduledEntryPlanningMode ParsePlanningMode(string planningMode)
    {
        return planningMode.Trim().ToLowerInvariant() switch
        {
            "onetime" => ScheduledEntryPlanningMode.OneTime,
            "recurring" => ScheduledEntryPlanningMode.Recurring,
            _ => throw new AppValidationException("O modo de planejamento informado e invalido.")
        };
    }

    private static ScheduledEntryRecurrenceFrequency? ParseNullableRecurrenceFrequency(string? recurrenceFrequency)
    {
        if (string.IsNullOrWhiteSpace(recurrenceFrequency))
        {
            return null;
        }

        return recurrenceFrequency.Trim().ToLowerInvariant() switch
        {
            "weekly" => ScheduledEntryRecurrenceFrequency.Weekly,
            "monthly" => ScheduledEntryRecurrenceFrequency.Monthly,
            _ => throw new AppValidationException("A frequencia recorrente informada e invalida.")
        };
    }

    private static ScheduledEntryStatus? ParseNullableStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        return status.Trim().ToLowerInvariant() switch
        {
            "scheduled" => ScheduledEntryStatus.Scheduled,
            "completed" => ScheduledEntryStatus.Completed,
            "skipped" => ScheduledEntryStatus.Skipped,
            "cancelled" => ScheduledEntryStatus.Cancelled,
            _ => throw new AppValidationException("O status do lancamento planejado informado e invalido.")
        };
    }

    private static ScheduledEntryResponse MapResponse(ScheduledEntryDto scheduledEntry)
    {
        return new ScheduledEntryResponse(
            scheduledEntry.Id,
            scheduledEntry.FinancialAccountId,
            scheduledEntry.FinancialAccountName,
            scheduledEntry.TransactionCategoryId,
            scheduledEntry.TransactionCategoryName,
            MapType(scheduledEntry.Type),
            MapPlanningMode(scheduledEntry.PlanningMode),
            MapNullableRecurrenceFrequency(scheduledEntry.RecurrenceFrequency),
            scheduledEntry.Amount,
            scheduledEntry.Description,
            scheduledEntry.StartDate,
            scheduledEntry.NextOccurrenceDate,
            scheduledEntry.EndDate,
            MapStatus(scheduledEntry.Status),
            scheduledEntry.LastRealizedAtUtc,
            scheduledEntry.CreatedAtUtc);
    }

    private static ScheduledEntryOccurrenceResponse MapOccurrenceResponse(ScheduledEntryOccurrenceDto scheduledEntryOccurrence)
    {
        return new ScheduledEntryOccurrenceResponse(
            scheduledEntryOccurrence.OccurrenceKey,
            scheduledEntryOccurrence.ScheduledEntryId,
            scheduledEntryOccurrence.FinancialAccountId,
            scheduledEntryOccurrence.FinancialAccountName,
            scheduledEntryOccurrence.TransactionCategoryId,
            scheduledEntryOccurrence.TransactionCategoryName,
            MapType(scheduledEntryOccurrence.Type),
            MapPlanningMode(scheduledEntryOccurrence.PlanningMode),
            MapNullableRecurrenceFrequency(scheduledEntryOccurrence.RecurrenceFrequency),
            scheduledEntryOccurrence.Amount,
            scheduledEntryOccurrence.Description,
            scheduledEntryOccurrence.StartDate,
            scheduledEntryOccurrence.OccurrenceDate,
            scheduledEntryOccurrence.NextOccurrenceDate,
            scheduledEntryOccurrence.EndDate,
            MapStatus(scheduledEntryOccurrence.Status),
            scheduledEntryOccurrence.TreatedAtUtc,
            scheduledEntryOccurrence.CanEdit,
            scheduledEntryOccurrence.CanAct,
            scheduledEntryOccurrence.CreatedAtUtc);
    }

    private static string MapType(TransactionType type)
    {
        return type switch
        {
            TransactionType.Income => "income",
            TransactionType.Expense => "expense",
            _ => throw new AppValidationException("O tipo do lancamento planejado nao e suportado.")
        };
    }

    private static string MapPlanningMode(ScheduledEntryPlanningMode planningMode)
    {
        return planningMode switch
        {
            ScheduledEntryPlanningMode.OneTime => "oneTime",
            ScheduledEntryPlanningMode.Recurring => "recurring",
            _ => throw new AppValidationException("O modo de planejamento informado nao e suportado.")
        };
    }

    private static string? MapNullableRecurrenceFrequency(ScheduledEntryRecurrenceFrequency? recurrenceFrequency)
    {
        return recurrenceFrequency switch
        {
            null => null,
            ScheduledEntryRecurrenceFrequency.Weekly => "weekly",
            ScheduledEntryRecurrenceFrequency.Monthly => "monthly",
            _ => throw new AppValidationException("A frequencia recorrente informada nao e suportada.")
        };
    }

    private static string MapStatus(ScheduledEntryStatus status)
    {
        return status switch
        {
            ScheduledEntryStatus.Scheduled => "scheduled",
            ScheduledEntryStatus.Completed => "completed",
            ScheduledEntryStatus.Skipped => "skipped",
            ScheduledEntryStatus.Cancelled => "cancelled",
            _ => throw new AppValidationException("O status do lancamento planejado nao e suportado.")
        };
    }
}
