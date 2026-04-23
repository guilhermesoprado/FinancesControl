using System.Security.Claims;
using FinanceManager.Api.Contracts.Responses.AuditLogs;
using FinanceManager.Application.AuditLogs;
using FinanceManager.Application.AuditLogs.Contracts;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AuditLogResponse>>> Get(
        [FromQuery] string? entityType,
        [FromQuery] string? action,
        [FromQuery] Guid? entityId,
        [FromQuery] string? search,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        var auditLogs = await _auditLogService.GetByUserAsync(
            new GetAuditLogsInput(
                GetAuthenticatedUserId(),
                MapEntityType(entityType),
                MapAction(action),
                entityId,
                search,
                from,
                to,
                limit ?? 100),
            cancellationToken);

        return Ok(auditLogs.Select(MapResponse).ToList());
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

    private static AuditLogEntityType? MapEntityType(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return null;
        }

        return entityType.Trim().ToLowerInvariant() switch
        {
            "financial_account" => AuditLogEntityType.FinancialAccount,
            "transaction_category" => AuditLogEntityType.TransactionCategory,
            _ => throw new AppValidationException("O tipo de entidade da auditoria informado e invalido.")
        };
    }

    private static AuditLogAction? MapAction(string? action)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            return null;
        }

        return action.Trim().ToLowerInvariant() switch
        {
            "created" => AuditLogAction.Created,
            "updated" => AuditLogAction.Updated,
            "inactivated" => AuditLogAction.Inactivated,
            _ => throw new AppValidationException("A acao da auditoria informada e invalida.")
        };
    }

    private static AuditLogResponse MapResponse(AuditLogDto auditLog)
    {
        return new AuditLogResponse(
            auditLog.Id,
            MapEntityType(auditLog.EntityType),
            auditLog.EntityId,
            MapAction(auditLog.Action),
            auditLog.Summary,
            auditLog.CreatedAtUtc);
    }

    private static string MapEntityType(AuditLogEntityType entityType)
    {
        return entityType switch
        {
            AuditLogEntityType.FinancialAccount => "financial_account",
            AuditLogEntityType.TransactionCategory => "transaction_category",
            _ => throw new AppValidationException("O tipo de entidade da auditoria nao e suportado.")
        };
    }

    private static string MapAction(AuditLogAction action)
    {
        return action switch
        {
            AuditLogAction.Created => "created",
            AuditLogAction.Updated => "updated",
            AuditLogAction.Inactivated => "inactivated",
            _ => throw new AppValidationException("A acao da auditoria nao e suportada.")
        };
    }
}
