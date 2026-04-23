using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.AuditLogs;
using FinanceManager.Application.AuditLogs.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class AuditLogsControllerTests
{
    [Fact]
    public async Task Get_ShouldForwardFiltersToService()
    {
        var userId = Guid.NewGuid();
        var service = new FakeAuditLogService
        {
            Result =
            [
                new AuditLogDto(
                    Guid.NewGuid(),
                    userId,
                    AuditLogEntityType.FinancialAccount,
                    Guid.NewGuid(),
                    AuditLogAction.Updated,
                    "Conta atualizada",
                    new DateTime(2026, 4, 23, 10, 0, 0, DateTimeKind.Utc))
            ]
        };
        var controller = CreateController(service, userId);

        var actionResult = await controller.Get(
            "financial_account",
            "updated",
            null,
            null,
            new DateOnly(2026, 4, 1),
            new DateOnly(2026, 4, 30),
            25,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        _ = Assert.IsAssignableFrom<IReadOnlyList<FinanceManager.Api.Contracts.Responses.AuditLogs.AuditLogResponse>>(okResult.Value);
        Assert.NotNull(service.LastInput);
        Assert.Equal(userId, service.LastInput!.UserId);
        Assert.Equal(AuditLogEntityType.FinancialAccount, service.LastInput.EntityType);
        Assert.Equal(AuditLogAction.Updated, service.LastInput.Action);
        Assert.Equal(25, service.LastInput.Limit);
    }

    [Fact]
    public async Task Get_ShouldForwardEntityIdAndSearchToService()
    {
        var userId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var service = new FakeAuditLogService();
        var controller = CreateController(service, userId);

        _ = await controller.Get(
            null,
            null,
            entityId,
            "principal",
            null,
            null,
            null,
            CancellationToken.None);

        Assert.NotNull(service.LastInput);
        Assert.Equal(entityId, service.LastInput!.EntityId);
        Assert.Equal("principal", service.LastInput.Search);
        Assert.Equal(100, service.LastInput.Limit);
    }

    private static AuditLogsController CreateController(IAuditLogService service, Guid userId)
    {
        return new AuditLogsController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(
                        new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                            "TestAuth"))
                }
            }
        };
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public GetAuditLogsInput? LastInput { get; private set; }
        public IReadOnlyList<AuditLogDto> Result { get; set; } = [];

        public Task<IReadOnlyList<AuditLogDto>> GetByUserAsync(GetAuditLogsInput input, CancellationToken cancellationToken)
        {
            LastInput = input;
            return Task.FromResult(Result);
        }
    }
}
