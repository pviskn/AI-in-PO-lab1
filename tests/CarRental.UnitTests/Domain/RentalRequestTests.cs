using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace CarRental.UnitTests.Domain;

public class RentalRequestTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void RentalRequest_ShouldBePending_WhenCreated()
    {
        RentalRequest request = CreateRequest();

        request.Status.Should().Be(RentalRequestStatus.Pending);
    }

    [Fact]
    public void RentalRequest_ShouldSetDates_WhenCreatedWithValidData()
    {
        DateOnly start = Today.AddDays(2);
        DateOnly end = Today.AddDays(9);

        RentalRequest request = CreateRequest(start, end);

        request.StartDate.Should().Be(start);
        request.EndDate.Should().Be(end);
    }

    [Fact]
    public void Approve_ShouldSetApprovedStatus_WhenRequestIsPending()
    {
        RentalRequest request = CreateRequest();

        request.Approve();

        request.Status.Should().Be(RentalRequestStatus.Approved);
    }

    [Fact]
    public void Approve_ShouldThrow_WhenRequestIsAlreadyApproved()
    {
        RentalRequest request = CreateRequest();
        request.Approve();

        Action act = () => request.Approve();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_ShouldThrow_WhenRequestIsRejected()
    {
        RentalRequest request = CreateRequest();
        request.Reject("тест");

        Action act = () => request.Approve();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_ShouldSetRejectedStatusAndComment_WhenRequestIsPending()
    {
        RentalRequest request = CreateRequest();
        const string comment = "Автомобиль недоступен";

        request.Reject(comment);

        request.Status.Should().Be(RentalRequestStatus.Rejected);
        request.ManagerComment.Should().Be(comment);
    }

    [Fact]
    public void Reject_ShouldThrow_WhenRequestIsAlreadyApproved()
    {
        RentalRequest request = CreateRequest();
        request.Approve();

        Action act = () => request.Reject("причина");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_ShouldSetCompletedStatus_WhenRequestIsApproved()
    {
        RentalRequest request = CreateRequest();
        request.Approve();

        request.Complete();

        request.Status.Should().Be(RentalRequestStatus.Completed);
    }

    [Fact]
    public void Complete_ShouldThrow_WhenRequestIsPending()
    {
        RentalRequest request = CreateRequest();

        Action act = () => request.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_ShouldThrow_WhenRequestIsRejected()
    {
        RentalRequest request = CreateRequest();
        request.Reject("отказ");

        Action act = () => request.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RentalRequest_ShouldThrow_WhenStartDateIsInPast()
    {
        DateOnly pastDate = Today.AddDays(-1);

        Action act = () => CreateRequest(pastDate, Today.AddDays(7));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RentalRequest_ShouldThrow_WhenEndDateIsBeforeStartDate()
    {
        DateOnly start = Today.AddDays(5);
        DateOnly end = Today.AddDays(3);

        Action act = () => CreateRequest(start, end);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RentalRequest_ShouldSucceed_WhenEndDateEqualsStartDate()
    {
        DateOnly date = Today.AddDays(5);

        RentalRequest request = CreateRequest(date, date);

        request.Should().NotBeNull();
        request.StartDate.Should().Be(date);
        request.EndDate.Should().Be(date);
    }

    [Fact]
    public void Cancel_ShouldSetCancelledStatus_WhenRequestIsPendingAndClientIsOwner()
    {
        RentalRequest request = CreateRequest();

        request.Cancel(request.UserId);

        request.Status.Should().Be(RentalRequestStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenClientIsNotOwner()
    {
        RentalRequest request = CreateRequest();

        Action act = () => request.Cancel(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
        request.Status.Should().Be(RentalRequestStatus.Pending);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenRequestIsApproved()
    {
        RentalRequest request = CreateRequest();
        request.Approve();

        Action act = () => request.Cancel(request.UserId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenRequestIsAlreadyCancelled()
    {
        RentalRequest request = CreateRequest();
        request.Cancel(request.UserId);

        Action act = () => request.Cancel(request.UserId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Approve_ShouldThrow_WhenRequestIsCancelled()
    {
        RentalRequest request = CreateRequest();
        request.Cancel(request.UserId);

        Action act = () => request.Approve();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reject_ShouldThrow_WhenRequestIsCancelled()
    {
        RentalRequest request = CreateRequest();
        request.Cancel(request.UserId);

        Action act = () => request.Reject("причина");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Complete_ShouldThrow_WhenRequestIsCancelled()
    {
        RentalRequest request = CreateRequest();
        request.Cancel(request.UserId);

        Action act = () => request.Complete();

        act.Should().Throw<InvalidOperationException>();
    }

    private static RentalRequest CreateRequest(DateOnly? start = null, DateOnly? end = null)
    {
        DateOnly startDate = start ?? Today.AddDays(1);
        DateOnly endDate = end ?? Today.AddDays(8);
        return new RentalRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), startDate, endDate);
    }
}