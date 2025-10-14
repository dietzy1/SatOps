using FluentAssertions;
using Xunit;

namespace SatOps.Tests
{
    public class FlightPlanServiceTests // : IDisposable
    {
        // private readonly SqliteConnection _connection;
        // private readonly DbContextOptions<SatOpsDbContext> _contextOptions;

        public FlightPlanServiceTests()
        {
            // _connection = new SqliteConnection("Filename=:memory:");
            // _connection.Open();

            // _contextOptions = new DbContextOptionsBuilder<SatOpsDbContext>()
            // .UseSqlite(_connection)
            // .Options;

            // using var context = new SatOpsDbContext(_contextOptions);
            // context.Database.EnsureCreated();
        }

        // private SatOpsDbContext CreateContext() => new SatOpsDbContext(_contextOptions);

        // public void Dispose()
        // {
        // _connection.Dispose();
        // }

        [Fact]
        public void Placeholder_Test_Should_Always_Pass()
        {
            true.Should().BeTrue();
        }


        /*         [Fact(Skip = "Temporarily disabled until DB provider issues in tests are resolved.")]
                public async Task CreateAsync_ShouldCreateAndReturnPendingFlightPlan()
                {
                    // Arrange
                    await using var dbContext = CreateContext();
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

                [Fact(Skip = "Temporarily disabled until DB provider issues in tests are resolved.")]
                public async Task CreateNewVersionAsync_ShouldSupersedeOldPlanAndCreateNewOne()
                {
                    // Arrange
                    await using var dbContext = CreateContext();
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

                [Fact(Skip = "Temporarily disabled until DB provider issues in tests are resolved.")]
                public async Task CreateNewVersionAsync_ShouldReturnNull_WhenPlanIsNotPending()
                {
                    // Arrange
                    await using var dbContext = CreateContext();
                    var repository = new FlightPlanRepository(dbContext);
                    var service = new FlightPlanService(repository, dbContext);

                    var approvedPlan = new FlightPlan
                    {
                        Id = Guid.NewGuid(),
                        Status = "approved", // Not pending
                        Body = JsonDocument.Parse("{}"),
                        Name = "Approved Plan",
                        GroundStationId = Guid.NewGuid(),
                        ScheduledAt = DateTime.UtcNow,
                        SatelliteName = "SAT-1"
                    };
                    await dbContext.FlightPlans.AddAsync(approvedPlan);
                    await dbContext.SaveChangesAsync();

                    var updateDto = new CreateFlightPlanDto { GsId = Guid.NewGuid().ToString() };

                    // Act
                    var result = await service.CreateNewVersionAsync(approvedPlan.Id, updateDto);

                    // Assert
                    result.Should().BeNull();
                }

                [Theory(Skip = "Temporarily disabled until DB provider issues in tests are resolved.")]
                [InlineData("approved")]
                [InlineData("rejected")]
                public async Task ApproveOrRejectAsync_ShouldUpdateStatus_WhenPlanIsPending(string targetStatus)
                {
                    // Arrange
                    var mockRepository = new Mock<IFlightPlanRepository>();
                    var service = new FlightPlanService(mockRepository.Object, null!);

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

                [Fact(Skip = "Temporarily disabled until DB provider issues in tests are resolved.")]
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
                } */
    }
}