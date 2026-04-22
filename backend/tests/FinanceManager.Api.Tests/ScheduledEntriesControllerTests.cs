using System.Security.Claims;
using FinanceManager.Api.Controllers;
using FinanceManager.Application.Common.Exceptions;
using FinanceManager.Application.ScheduledEntries;
using FinanceManager.Application.ScheduledEntries.Contracts;
using FinanceManager.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FinanceManager.Api.Tests;

public sealed class ScheduledEntriesControllerTests
{
    [Fact]
    public async Task Get_ShouldReturnMappedResponses()
    {
        var userId = Guid.NewGuid();
        var scheduledEntry = new ScheduledEntryOccurrenceDto(
            "occ-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Conta",
            Guid.NewGuid(),
            "Internet",
            TransactionType.Expense,
            ScheduledEntryPlanningMode.Recurring,
            ScheduledEntryRecurrenceFrequency.Monthly,
            150m,
            "Internet casa",
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 6, 10),
            null,
            ScheduledEntryStatus.Scheduled,
            null,
            true,
            true,
            new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc));
        var service = new FakeScheduledEntryService
        {
            ScheduledEntriesToReturn = [scheduledEntry]
        };
        var controller = CreateController(service, userId);

        var actionResult = await controller.Get("scheduled", new DateOnly(2026, 5, 1), new DateOnly(2026, 6, 30), CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var payload = Assert.IsAssignableFrom<IReadOnlyList<FinanceManager.Api.Contracts.Responses.ScheduledEntries.ScheduledEntryOccurrenceResponse>>(okResult.Value);
        var response = Assert.Single(payload);

        Assert.Equal("expense", response.Type);
        Assert.Equal("recurring", response.PlanningMode);
        Assert.Equal("monthly", response.RecurrenceFrequency);
        Assert.Equal("scheduled", response.Status);
        Assert.True(response.CanAct);
        Assert.Equal(new DateOnly(2026, 5, 10), response.OccurrenceDate);
        Assert.NotNull(service.LastGetInput);
        Assert.Equal(ScheduledEntryStatus.Scheduled, service.LastGetInput!.Status);
        Assert.Equal(userId, service.LastGetInput.UserId);
    }

    [Fact]
    public async Task Get_ShouldRejectUnknownStatus()
    {
        var controller = CreateController(new FakeScheduledEntryService(), Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<AppValidationException>(() => controller.Get(
            "invalid-status",
            null,
            null,
            CancellationToken.None));

        Assert.Equal("O status do lancamento planejado informado e invalido.", exception.Message);
    }

    [Fact]
    public async Task Update_ShouldForwardParsedPayloadToService()
    {
        var userId = Guid.NewGuid();
        var scheduledEntryId = Guid.NewGuid();
        var scheduledEntry = new ScheduledEntryDto(
            scheduledEntryId,
            Guid.NewGuid(),
            "Conta editada",
            Guid.NewGuid(),
            "Categoria editada",
            TransactionType.Expense,
            ScheduledEntryPlanningMode.Recurring,
            ScheduledEntryRecurrenceFrequency.Weekly,
            200m,
            "Previsto editado",
            new DateOnly(2026, 5, 20),
            new DateOnly(2026, 5, 20),
            new DateOnly(2026, 7, 20),
            ScheduledEntryStatus.Scheduled,
            null,
            new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc));
        var service = new FakeScheduledEntryService
        {
            UpdatedScheduledEntry = scheduledEntry,
        };
        var controller = CreateController(service, userId);

        var actionResult = await controller.Update(
            scheduledEntryId,
            new FinanceManager.Api.Contracts.Requests.ScheduledEntries.UpdateScheduledEntryRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "recurring",
                "weekly",
                200m,
                "Previsto editado",
                new DateOnly(2026, 5, 20),
                new DateOnly(2026, 7, 20)),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<FinanceManager.Api.Contracts.Responses.ScheduledEntries.ScheduledEntryResponse>(okResult.Value);

        Assert.Equal("recurring", response.PlanningMode);
        Assert.Equal("weekly", response.RecurrenceFrequency);
        Assert.NotNull(service.LastUpdateInput);
        Assert.Equal(userId, service.LastUpdateInput!.UserId);
        Assert.Equal(scheduledEntryId, service.LastUpdateInput.ScheduledEntryId);
        Assert.Equal(ScheduledEntryPlanningMode.Recurring, service.LastUpdateInput.PlanningMode);
        Assert.Equal(ScheduledEntryRecurrenceFrequency.Weekly, service.LastUpdateInput.RecurrenceFrequency);
    }

    [Fact]
    public async Task Complete_ShouldForwardOccurrenceDateToService()
    {
        var userId = Guid.NewGuid();
        var scheduledEntryId = Guid.NewGuid();
        var scheduledEntry = new ScheduledEntryDto(
            scheduledEntryId,
            Guid.NewGuid(),
            "Conta",
            Guid.NewGuid(),
            "Internet",
            TransactionType.Expense,
            ScheduledEntryPlanningMode.Recurring,
            ScheduledEntryRecurrenceFrequency.Monthly,
            150m,
            "Internet",
            new DateOnly(2026, 5, 10),
            new DateOnly(2026, 6, 10),
            null,
            ScheduledEntryStatus.Scheduled,
            null,
            new DateTime(2026, 4, 18, 12, 0, 0, DateTimeKind.Utc));
        var service = new FakeScheduledEntryService
        {
            CompletedScheduledEntry = scheduledEntry,
        };
        var controller = CreateController(service, userId);

        var actionResult = await controller.Complete(
            scheduledEntryId,
            new FinanceManager.Api.Contracts.Requests.ScheduledEntries.ScheduledEntryOccurrenceActionRequest(new DateOnly(2026, 5, 10)),
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        _ = Assert.IsType<FinanceManager.Api.Contracts.Responses.ScheduledEntries.ScheduledEntryResponse>(okResult.Value);
        Assert.NotNull(service.LastActionInput);
        Assert.Equal(userId, service.LastActionInput!.UserId);
        Assert.Equal(scheduledEntryId, service.LastActionInput.ScheduledEntryId);
        Assert.Equal(new DateOnly(2026, 5, 10), service.LastActionInput.OccurrenceDate);
    }

    private static ScheduledEntriesController CreateController(IScheduledEntryService scheduledEntryService, Guid userId)
    {
        return new ScheduledEntriesController(scheduledEntryService)
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

    private sealed class FakeScheduledEntryService : IScheduledEntryService
    {
        public IReadOnlyList<ScheduledEntryOccurrenceDto> ScheduledEntriesToReturn { get; set; } = [];
        public GetScheduledEntriesInput? LastGetInput { get; private set; }
        public UpdateScheduledEntryInput? LastUpdateInput { get; private set; }
        public ApplyScheduledEntryOccurrenceActionInput? LastActionInput { get; private set; }
        public ScheduledEntryDto? UpdatedScheduledEntry { get; set; }
        public ScheduledEntryDto? CompletedScheduledEntry { get; set; }

        public Task<ScheduledEntryDto> CreateAsync(CreateScheduledEntryInput input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ScheduledEntryDto> UpdateAsync(UpdateScheduledEntryInput input, CancellationToken cancellationToken)
        {
            LastUpdateInput = input;
            return Task.FromResult(UpdatedScheduledEntry!);
        }

        public Task<IReadOnlyList<ScheduledEntryOccurrenceDto>> GetByUserAsync(GetScheduledEntriesInput input, CancellationToken cancellationToken)
        {
            LastGetInput = input;
            return Task.FromResult(ScheduledEntriesToReturn);
        }

        public Task<ScheduledEntryDto> CompleteAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken)
        {
            LastActionInput = input;
            return Task.FromResult(CompletedScheduledEntry!);
        }

        public Task<ScheduledEntryDto> SkipAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ScheduledEntryDto> CancelAsync(ApplyScheduledEntryOccurrenceActionInput input, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
