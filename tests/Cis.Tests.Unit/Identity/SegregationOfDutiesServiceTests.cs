using Cis.Application.Common.Exceptions;
using Cis.Application.Identity;
using FluentAssertions;

namespace Cis.Tests.Unit.Identity;

public sealed class SegregationOfDutiesServiceTests
{
    [Fact]
    public void EnsureCanDecide_WhenInitiatorIsApprover_ThrowsValidationException()
    {
        var service = new SegregationOfDutiesService();

        var action = () => service.EnsureCanDecide("access change", "user-001", "user-001");

        action.Should().Throw<ValidationException>();
    }

    [Fact]
    public void EnsureCanDecide_WhenApproverIsDifferent_DoesNotThrow()
    {
        var service = new SegregationOfDutiesService();

        var action = () => service.EnsureCanDecide("access change", "user-001", "user-002");

        action.Should().NotThrow();
    }
}
