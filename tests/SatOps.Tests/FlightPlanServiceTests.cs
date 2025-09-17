using Microsoft.EntityFrameworkCore;
using SatOps.Controllers.FlightPlan;
using SatOps.Services;
using SatOps.Services.FlightPlan;
using System.Text.Json;
using Moq;
using FluentAssertions;
using Xunit;

namespace SatOps.Tests
{
    public class FlightPlanServiceTests
    {
        private SatOpsDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<SatOpsDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB for each test
                .Options;
            var dbContext = new SatOpsDbContext(options);
            return dbContext;
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateAndReturnPendingFlightPlan()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new FlightPlanRepository(dbContext);
            var service = new FlightPlanService(repository, dbContext);

            var createDto = new CreateFlightPlanDto
            {
                GsId = Guid.NewGuid().ToString(),
                SatName = "ISS",
                ScheduledAt = DateTime.UtcNow.AddDays(1),
                FlightPlanBody = new FlightPlanBodyDto
                {
                    Name = "Test Plan",
                    Body = new { command = "test" }
                }
            };

            // Act
            var result = await service.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be("Test Plan");
            result.Status.Should().Be("pending");
            result.PreviousPlanId.Should().BeNull();

            var savedPlan = await dbContext.FlightPlans.FindAsync(result.Id);
            savedPlan.Should().NotBeNull();
            savedPlan!.Status.Should().Be("pending");
        }

        [Fact]
        public async Task CreateNewVersionAsync_ShouldSupersedeOldPlanAndCreateNewOne()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new FlightPlanRepository(dbContext);
            var service = new FlightPlanService(repository, dbContext);
            var groundStationId = Guid.NewGuid();

            var originalPlan = new FlightPlan
            {
                Id = Guid.NewGuid(),
                Name = "Original Plan",
                Status = "pending",
                GroundStationId = groundStationId,
                Body = JsonDocument.Parse("{}"),
                ScheduledAt = DateTime.UtcNow,
                SatelliteName = "SAT-1"
            };
            await dbContext.FlightPlans.AddAsync(originalPlan);
            await dbContext.SaveChangesAsync();

            var updateDto = new CreateFlightPlanDto
            {
                GsId = groundStationId.ToString(),
                SatName = "SAT-1 Updated",
                ScheduledAt = DateTime.UtcNow.AddHours(1),
                FlightPlanBody = new FlightPlanBodyDto
                {
                    Name = "Updated Plan",
                    Body = new { command = "update" }
                }
            };

            // Act
            var newVersion = await service.CreateNewVersionAsync(originalPlan.Id, updateDto);

            // Assert
            newVersion.Should().NotBeNull();
            newVersion!.Name.Should().Be("Updated Plan");
            newVersion.Status.Should().Be("pending");
            newVersion.PreviousPlanId.Should().Be(originalPlan.Id);
            newVersion.SatelliteName.Should().Be("SAT-1 Updated");

            var oldPlanInDb = await dbContext.FlightPlans.FindAsync(originalPlan.Id);
            oldPlanInDb.Should().NotBeNull();
            oldPlanInDb!.Status.Should().Be("superseded");
        }

        [Fact]
        public async Task CreateNewVersionAsync_ShouldReturnNull_WhenPlanIsNotPending()
        {
            // Arrange
            var dbContext = GetInMemoryDbContext();
            var repository = new FlightPlanRepository(dbContext);
            var service = new FlightPlanService(repository, dbContext);

            var approvedPlan = new FlightPlan
            {
                Id = Guid.NewGuid(),
                Status = "approved", // Not pending
                Body = JsonDocument.Parse("{}")
            };
            await dbContext.FlightPlans.AddAsync(approvedPlan);
            await dbContext.SaveChangesAsync();

            var updateDto = new CreateFlightPlanDto { GsId = Guid.NewGuid().ToString() };

            // Act
            var result = await service.CreateNewVersionAsync(approvedPlan.Id, updateDto);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("approved")]
        [InlineData("rejected")]
        public async Task ApproveOrRejectAsync_ShouldUpdateStatus_WhenPlanIsPending(string targetStatus)
        {
            // Arrange
            var mockRepository = new Mock<IFlightPlanRepository>();
            var service = new FlightPlanService(mockRepository.Object, null!); // DbContext not used in this method

            var planId = Guid.NewGuid();
            var pendingPlan = new FlightPlan
            {
                Id = planId,
                Status = "pending"
            };

            mockRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(pendingPlan);
            mockRepository.Setup(r => r.UpdateAsync(It.IsAny<FlightPlan>())).ReturnsAsync(true);

            // Act
            var (success, message) = await service.ApproveOrRejectAsync(planId, targetStatus);

            // Assert
            success.Should().BeTrue();
            message.Should().Be($"Flight plan successfully {targetStatus}.");

            mockRepository.Verify(r => r.UpdateAsync(It.Is<FlightPlan>(p =>
                p.Id == planId &&
                p.Status == targetStatus &&
                p.ApprovalDate.HasValue &&
                p.ApproverId == "mock-user-id"
            )), Times.Once);
        }

        [Fact]
        public async Task ApproveOrRejectAsync_ShouldFail_WhenPlanIsNotPending()
        {
            // Arrange
            var mockRepository = new Mock<IFlightPlanRepository>();
            var service = new FlightPlanService(mockRepository.Object, null!);

            var planId = Guid.NewGuid();
            var approvedPlan = new FlightPlan
            {
                Id = planId,
                Status = "approved"
            };

            mockRepository.Setup(r => r.GetByIdAsync(planId)).ReturnsAsync(approvedPlan);

            // Act
            var (success, message) = await service.ApproveOrRejectAsync(planId, "approved");

            // Assert
            success.Should().BeFalse();
            message.Should().Be("Cannot update a plan with status 'approved'.");
            mockRepository.Verify(r => r.UpdateAsync(It.IsAny<FlightPlan>()), Times.Never);
        }
    }
}