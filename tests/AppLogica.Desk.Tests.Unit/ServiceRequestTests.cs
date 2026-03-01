using AppLogica.Desk.Application.ServiceCatalog.Jobs;
using AppLogica.Desk.Domain.Repositories;
using AppLogica.Desk.Domain.ServiceCatalog;
using AppLogica.Desk.Domain.ServiceCatalog.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AppLogica.Desk.Tests.Unit;

public class ServiceRequestTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _catalogItemId = Guid.NewGuid();

    private ServiceRequest CreateTestServiceRequest(bool requiresApproval = false)
    {
        return ServiceRequest.Create(
            _tenantId,
            "SRQ-2026-00001",
            "Test Service Request",
            "Test description",
            _catalogItemId,
            _requesterId,
            requiresApproval);
    }

    // ─── Test 1: ServiceRequest lifecycle (submit, approve, fulfill) ───

    [Fact]
    public void CanTransition_FullLifecycle_SubmitApproveFulfill()
    {
        var request = CreateTestServiceRequest(requiresApproval: true);

        // Submit
        request.Submit();
        request.Status.Should().Be(ServiceRequestStatus.PendingApproval);

        // Approve
        request.Approve();
        request.Status.Should().Be(ServiceRequestStatus.Approved);
        request.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

        // Assign
        var assigneeId = Guid.NewGuid();
        request.Assign(assigneeId);
        request.Status.Should().Be(ServiceRequestStatus.InProgress);
        request.AssigneeId.Should().Be(assigneeId);

        // Fulfill
        request.Fulfill("Completed per instructions.");
        request.Status.Should().Be(ServiceRequestStatus.Fulfilled);
        request.FulfilledAt.Should().NotBeNull();
        request.FulfillmentNotes.Should().Be("Completed per instructions.");
    }

    // ─── Test 2: State machine guards (invalid transitions throw) ───

    [Fact]
    public void CannotTransition_FulfilledToApproved_ThrowsInvalidOperation()
    {
        var request = CreateTestServiceRequest(requiresApproval: false);
        request.Submit();
        request.Assign(Guid.NewGuid());
        request.Fulfill("Done.");

        var act = () => request.Approve();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot approve*Fulfilled*");
    }

    [Fact]
    public void CannotTransition_DraftToFulfill_ThrowsInvalidOperation()
    {
        var request = CreateTestServiceRequest();

        var act = () => request.Fulfill("Done.");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot fulfill*Draft*");
    }

    [Fact]
    public void CannotTransition_SubmittedToApprove_ThrowsInvalidOperation()
    {
        // Non-approval request goes to Submitted, not PendingApproval
        var request = CreateTestServiceRequest(requiresApproval: false);
        request.Submit();
        request.Status.Should().Be(ServiceRequestStatus.Submitted);

        var act = () => request.Approve();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot approve*Submitted*");
    }

    // ─── Test 3: Approval status transitions ───

    [Fact]
    public void ApprovalStatus_TransitionsCorrectly_ThroughLifecycle()
    {
        // No approval required
        var noApproval = CreateTestServiceRequest(requiresApproval: false);
        noApproval.ApprovalStatus.Should().Be(ApprovalStatus.NotRequired);

        // Requires approval
        var withApproval = CreateTestServiceRequest(requiresApproval: true);
        withApproval.ApprovalStatus.Should().Be(ApprovalStatus.Pending);

        withApproval.Submit();
        withApproval.Approve();
        withApproval.ApprovalStatus.Should().Be(ApprovalStatus.Approved);

        // Rejection
        var rejected = CreateTestServiceRequest(requiresApproval: true);
        rejected.Submit();
        rejected.Reject("Budget not available.");
        rejected.ApprovalStatus.Should().Be(ApprovalStatus.Rejected);
        rejected.CancellationReason.Should().Be("Budget not available.");

        // Timeout
        var timedOut = CreateTestServiceRequest(requiresApproval: true);
        timedOut.Submit();
        timedOut.TimeoutApproval();
        timedOut.ApprovalStatus.Should().Be(ApprovalStatus.TimedOut);
        timedOut.Status.Should().Be(ServiceRequestStatus.Cancelled);
    }

    // ─── Test 4: Request number format validation ───

    [Fact]
    public void RequestNumber_InvalidFormat_ThrowsArgumentException()
    {
        var invalidFormats = new[]
        {
            "INC-2026-00001",  // Wrong prefix
            "SRQ-2026-0001",   // Too short
            "SRQ-2026-000001", // Too long
            "SRQ202600001",    // Missing hyphens
            ""                 // Empty
        };

        foreach (var invalidFormat in invalidFormats)
        {
            var act = () => ServiceRequest.Create(
                _tenantId, invalidFormat, "Title", null, _catalogItemId, _requesterId, false);

            act.Should().Throw<ArgumentException>($"Format '{invalidFormat}' should be rejected");
        }
    }

    [Fact]
    public void RequestNumber_ValidFormat_Succeeds()
    {
        var request = ServiceRequest.Create(
            _tenantId, "SRQ-2026-00001", "Title", null, _catalogItemId, _requesterId, false);

        request.RequestNumber.Should().Be("SRQ-2026-00001");
    }

    // ─── Test 5: FulfillmentTask completion ───

    [Fact]
    public void FulfillmentTask_CanComplete_AndTrackCompletionDetails()
    {
        var requestId = Guid.NewGuid();
        var completedBy = Guid.NewGuid();

        var task = FulfillmentTask.Create(_tenantId, requestId, "Install software");
        task.Status.Should().Be(FulfillmentTaskStatus.Pending);

        task.StartProgress();
        task.Status.Should().Be(FulfillmentTaskStatus.InProgress);

        task.Complete(completedBy, "Installed Office 365.");
        task.Status.Should().Be(FulfillmentTaskStatus.Completed);
        task.CompletedAt.Should().NotBeNull();
        task.CompletedBy.Should().Be(completedBy);
        task.Notes.Should().Be("Installed Office 365.");
    }

    [Fact]
    public void FulfillmentTask_CanSkip_FromPendingOrInProgress()
    {
        var requestId = Guid.NewGuid();
        var skippedBy = Guid.NewGuid();

        var task = FulfillmentTask.Create(_tenantId, requestId, "Optional step");
        task.Skip(skippedBy, "Not applicable.");
        task.Status.Should().Be(FulfillmentTaskStatus.Skipped);

        // Cannot complete a skipped task
        var act = () => task.Complete(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>();
    }

    // ─── Test 6: ApprovalTimeoutJob logic ───

    [Fact]
    public async Task ApprovalTimeoutJob_TimesOut_PendingApprovals()
    {
        var mockRepo = new Mock<IServiceRequestRepository>();
        var mockLogger = new Mock<ILogger<ApprovalTimeoutJob>>();

        var timedOutRequest = CreateTestServiceRequest(requiresApproval: true);
        timedOutRequest.Submit();
        timedOutRequest.Status.Should().Be(ServiceRequestStatus.PendingApproval);

        mockRepo.Setup(r => r.GetTimedOutApprovalsAsync(_tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([timedOutRequest]);

        mockRepo.Setup(r => r.UpdateAsync(It.IsAny<ServiceRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var job = new ApprovalTimeoutJob(mockRepo.Object, mockLogger.Object);

        await job.ExecuteAsync(_tenantId);

        timedOutRequest.Status.Should().Be(ServiceRequestStatus.Cancelled);
        timedOutRequest.ApprovalStatus.Should().Be(ApprovalStatus.TimedOut);
        timedOutRequest.CancellationReason.Should().Be("Approval timed out.");

        mockRepo.Verify(r => r.UpdateAsync(timedOutRequest, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Test 7: ServiceCatalogItem factory validation ───

    [Fact]
    public void ServiceCatalogItem_Create_ValidatesRequiredFields()
    {
        // Name is required
        var actNullName = () => ServiceCatalogItem.Create(_tenantId, "", _categoryId);
        actNullName.Should().Throw<ArgumentException>();

        // ExpectedDeliveryMinutes must be positive
        var actZeroMinutes = () => ServiceCatalogItem.Create(
            _tenantId, "Item", _categoryId, expectedDeliveryMinutes: 0);
        actZeroMinutes.Should().Throw<ArgumentOutOfRangeException>();

        // RequiresApproval=true requires ApprovalWorkflowId
        var actNoWorkflow = () => ServiceCatalogItem.Create(
            _tenantId, "Item", _categoryId, requiresApproval: true, approvalWorkflowId: null);
        actNoWorkflow.Should().Throw<ArgumentException>()
            .WithMessage("*ApprovalWorkflowId*");

        // Valid creation
        var workflowId = Guid.NewGuid();
        var item = ServiceCatalogItem.Create(
            _tenantId, "Laptop Request", _categoryId,
            description: "Request a laptop",
            expectedDeliveryMinutes: 1440,
            requiresApproval: true,
            approvalWorkflowId: workflowId);

        item.Name.Should().Be("Laptop Request");
        item.RequiresApproval.Should().BeTrue();
        item.ApprovalWorkflowId.Should().Be(workflowId);
    }

    // ─── Test 8: Category hierarchy ───

    [Fact]
    public void CategoryHierarchy_SupportsParentChild()
    {
        var parentCategory = ServiceCatalogCategory.Create(
            _tenantId, "IT Services", "Top-level IT", null, 1);

        parentCategory.Name.Should().Be("IT Services");
        parentCategory.ParentCategoryId.Should().BeNull();
        parentCategory.IsActive.Should().BeTrue();

        var childCategory = ServiceCatalogCategory.Create(
            _tenantId, "Hardware", "Hardware requests", parentCategory.Id, 1);

        childCategory.Name.Should().Be("Hardware");
        childCategory.ParentCategoryId.Should().Be(parentCategory.Id);
    }

    // ─── Bonus: Domain events ───

    [Fact]
    public void DomainEvent_Raised_OnSubmit()
    {
        var request = CreateTestServiceRequest();

        request.Submit();

        request.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ServiceRequestSubmittedEvent>();

        var evt = (ServiceRequestSubmittedEvent)request.DomainEvents[0];
        evt.RequestId.Should().Be(request.Id);
        evt.TenantId.Should().Be(_tenantId);
        evt.RequestNumber.Should().Be("SRQ-2026-00001");
    }

    [Fact]
    public void DomainEvent_Raised_OnApproval()
    {
        var request = CreateTestServiceRequest(requiresApproval: true);
        request.Submit();
        request.ClearDomainEvents();

        request.Approve();

        request.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ServiceRequestApprovedEvent>();
    }

    [Fact]
    public void DomainEvent_Raised_OnFulfillment()
    {
        var request = CreateTestServiceRequest(requiresApproval: false);
        request.Submit();
        request.Assign(Guid.NewGuid());
        request.ClearDomainEvents();

        request.Fulfill("All done.");

        request.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ServiceRequestFulfilledEvent>();
    }
}
