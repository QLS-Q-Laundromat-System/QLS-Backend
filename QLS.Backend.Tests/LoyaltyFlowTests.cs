using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Loyalty;
using QLS.Backend.DTOs.Zalo;
using QLS.Backend.Exceptions;
using QLS.Backend.Models;
using QLS.Backend.Models.Enums;
using QLS.Backend.Services.Loyalty;
using QLS.Backend.Services.Zalo;
using Xunit;

namespace QLS.Backend.Tests;

public class LoyaltyFlowTests
{
    [Fact]
    public async Task PaymentToClaimFlow_ShouldCreateToken_LoginAndAwardPoints()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var paymentSession = await fixture.SeedPaidSessionAsync(amount: 25000m);
        var loyaltyService = fixture.CreateLoyaltyService();
        var zaloAuthService = fixture.CreateZaloAuthService();

        var token = await loyaltyService.EnsureClaimTokenForPaymentAsync(paymentSession.Session, paymentSession.Transaction);

        Assert.NotNull(token);
        Assert.Equal(2, token!.PointsToEarn);
        Assert.False(token.IsClaimed);

        var login = await zaloAuthService.LoginAsync(new ZaloLoginRequestDto
        {
            BrandId = paymentSession.Brand.Id,
            ZaloUserId = "zalo-user-001",
            ZaloOAUserId = "oa-001",
            FullName = "Test Customer",
            AvatarUrl = "https://example.com/avatar.png"
        });

        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal(0, login.TotalPoints);

        var claim = await loyaltyService.ClaimPointsAsync(
            login.CustomerId,
            new LoyaltyClaimRequestDto { ClaimToken = token.Token });

        Assert.Equal(2, claim.ClaimedPoints);
        Assert.Equal(2, claim.TotalPoints);

        var dbToken = await fixture.Context.PointClaimTokens.FirstAsync(x => x.Id == token.Id);
        Assert.True(dbToken.IsClaimed);
        Assert.Equal(login.CustomerId, dbToken.ClaimedByCustomerId);

        var earnTx = await fixture.Context.LoyaltyPointTransactions
            .FirstOrDefaultAsync(x => x.MachineSessionId == paymentSession.Session.Id && x.Type == PointTransactionType.Earn);

        Assert.NotNull(earnTx);
        Assert.Equal(2, earnTx!.Points);
    }

    [Fact]
    public async Task ClaimedSession_WhenRollback_ShouldDeductPointsAndCreateAdjustTransaction()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var paymentSession = await fixture.SeedPaidSessionAsync(amount: 50000m);
        var loyaltyService = fixture.CreateLoyaltyService();
        var zaloAuthService = fixture.CreateZaloAuthService();

        var token = await loyaltyService.EnsureClaimTokenForPaymentAsync(paymentSession.Session, paymentSession.Transaction);
        Assert.NotNull(token);

        var login = await zaloAuthService.LoginAsync(new ZaloLoginRequestDto
        {
            BrandId = paymentSession.Brand.Id,
            ZaloUserId = "zalo-user-rollback",
            FullName = "Rollback User"
        });

        await loyaltyService.ClaimPointsAsync(login.CustomerId, new LoyaltyClaimRequestDto { ClaimToken = token!.Token });

        var rolledBack = await loyaltyService.RollbackEarnedPointsForSessionAsync(paymentSession.Session.Id, "Session Error");
        Assert.True(rolledBack);

        var customer = await fixture.Context.LoyaltyCustomers.FirstAsync(c => c.Id == login.CustomerId);
        Assert.Equal(0, customer.TotalPoints);

        var adjustTx = await fixture.Context.LoyaltyPointTransactions
            .FirstOrDefaultAsync(x => x.MachineSessionId == paymentSession.Session.Id && x.Type == PointTransactionType.Adjust);

        Assert.NotNull(adjustTx);
        Assert.Equal(-5, adjustTx!.Points);
    }

    [Fact]
    public async Task ClaimingSameTokenTwice_ShouldThrowConflictApiException()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var paymentSession = await fixture.SeedPaidSessionAsync(amount: 30000m);
        var loyaltyService = fixture.CreateLoyaltyService();
        var zaloAuthService = fixture.CreateZaloAuthService();

        var token = await loyaltyService.EnsureClaimTokenForPaymentAsync(paymentSession.Session, paymentSession.Transaction);
        var login = await zaloAuthService.LoginAsync(new ZaloLoginRequestDto
        {
            BrandId = paymentSession.Brand.Id,
            ZaloUserId = "zalo-user-reuse",
            FullName = "Reuse User"
        });

        await loyaltyService.ClaimPointsAsync(login.CustomerId, new LoyaltyClaimRequestDto { ClaimToken = token!.Token });

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            loyaltyService.ClaimPointsAsync(login.CustomerId, new LoyaltyClaimRequestDto { ClaimToken = token.Token }));

        Assert.Equal(409, ex.StatusCode);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AppDbContext Context { get; }
        public IConfiguration Configuration { get; }

        private TestFixture(SqliteConnection connection, AppDbContext context, IConfiguration configuration)
        {
            _connection = connection;
            Context = context;
            Configuration = configuration;
        }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new AppDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "TestJwtKeyForLoyaltyFlow_1234567890",
                    ["Jwt:Issuer"] = "TestIssuer",
                    ["Jwt:Audience"] = "TestAudience",
                    ["Jwt:ExpireMinutes"] = "60",
                    ["Loyalty:ClaimTokenTtlMinutes"] = "10",
                    ["Loyalty:PointUnitVnd"] = "10000",
                    ["Loyalty:PointExpiryMonths"] = "3"
                })
                .Build();

            return new TestFixture(connection, context, config);
        }

        public LoyaltyService CreateLoyaltyService() => new(Context, Configuration);
        public ZaloAuthService CreateZaloAuthService() => new(Context, Configuration);

        public async Task<(Brand Brand, Store Store, User User, Machine Machine, MachineSession Session, PaymentTransaction Transaction)> SeedPaidSessionAsync(decimal amount)
        {
            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                Name = "Test Brand",
                Email = "brand@test.local",
                IsActive = true
            };

            var store = new Store
            {
                Id = Guid.NewGuid(),
                BrandId = brand.Id,
                Name = "Test Store",
                Address = "Addr",
                Phone = "0123",
                Email = "store@test.local",
                IsActive = true
            };

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = "System User",
                Email = "user@test.local",
                IsActive = true
            };

            var machine = new Machine
            {
                Id = Guid.NewGuid(),
                StoreId = store.Id,
                Name = "Machine 01",
                Type = MachineType.Washer,
                Capacity = "10kg"
            };

            var now = DateTime.UtcNow;
            var session = new MachineSession
            {
                Id = Guid.NewGuid(),
                MachineId = machine.Id,
                StoreId = store.Id,
                UserId = user.Id,
                PricePaid = amount,
                TaxAmount = 0,
                TotalMinutes = 30,
                StartTime = now,
                EndTime = now.AddMinutes(30),
                Status = MachineSessionStatus.PaidWaitingForStart,
                CreatedAt = now,
                UpdatedAt = now,
                PaymentConfirmedAt = now,
                PaymentCode = $"QLS{Guid.NewGuid():N}"[..8]
            };

            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                MachineSessionId = session.Id,
                Amount = amount,
                PaymentMethod = "SePay",
                GatewayTransactionId = Guid.NewGuid().ToString("N"),
                Status = "Success",
                CreatedAt = now
            };

            Context.Brands.Add(brand);
            Context.Stores.Add(store);
            Context.Users.Add(user);
            Context.Machines.Add(machine);
            Context.MachineSessions.Add(session);
            Context.PaymentTransactions.Add(transaction);
            await Context.SaveChangesAsync();

            return (brand, store, user, machine, session, transaction);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
