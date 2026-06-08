using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QLS.Backend.Data;
using QLS.Backend.DTOs.Loyalty;
using QLS.Backend.DTOs.Zalo;
using QLS.Backend.Exceptions;
using QLS.Backend.Interfaces.Zalo;
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

        var login = await fixture.LoginCustomerAsync(
            zaloAuthService,
            paymentSession.Brand.Id,
            "zalo-user-001",
            "Test Customer");

        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal(100, login.TotalPoints);

        var claim = await loyaltyService.ClaimPointsAsync(
            login.CustomerId,
            new LoyaltyClaimRequestDto { ClaimToken = token.Token });

        Assert.Equal(2, claim.ClaimedPoints);
        Assert.Equal(102, claim.TotalPoints);

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

        var login = await fixture.LoginCustomerAsync(
            zaloAuthService,
            paymentSession.Brand.Id,
            "zalo-user-rollback",
            "Rollback User");

        await loyaltyService.ClaimPointsAsync(login.CustomerId, new LoyaltyClaimRequestDto { ClaimToken = token!.Token });

        var rolledBack = await loyaltyService.RollbackEarnedPointsForSessionAsync(paymentSession.Session.Id, "Session Error");
        Assert.True(rolledBack);

        var customer = await fixture.Context.LoyaltyCustomers.FirstAsync(c => c.Id == login.CustomerId);
        Assert.Equal(100, customer.TotalPoints);

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
        var login = await fixture.LoginCustomerAsync(
            zaloAuthService,
            paymentSession.Brand.Id,
            "zalo-user-reuse",
            "Reuse User");

        await loyaltyService.ClaimPointsAsync(login.CustomerId, new LoyaltyClaimRequestDto { ClaimToken = token!.Token });

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            loyaltyService.ClaimPointsAsync(login.CustomerId, new LoyaltyClaimRequestDto { ClaimToken = token.Token }));

        Assert.Equal(409, ex.StatusCode);
    }

    [Fact]
    public async Task ZaloLogin_ShouldUseVerifiedProfileAndUpdateExistingCustomer()
    {
        await using var fixture = await TestFixture.CreateAsync();
        var paymentSession = await fixture.SeedPaidSessionAsync(amount: 10000m);
        var zaloAuthService = fixture.CreateZaloAuthService();

        var firstLogin = await fixture.LoginCustomerAsync(
            zaloAuthService,
            paymentSession.Brand.Id,
            "zalo-profile-user",
            "Initial Name");
        var secondLogin = await fixture.LoginCustomerAsync(
            zaloAuthService,
            paymentSession.Brand.Id,
            "zalo-profile-user",
            "Updated Name");

        var customer = await fixture.Context.LoyaltyCustomers.FirstAsync(c => c.Id == firstLogin.CustomerId);
        Assert.Equal(firstLogin.CustomerId, secondLogin.CustomerId);
        Assert.Equal("Updated Name", customer.FullName);
        Assert.Equal("https://example.com/zalo-profile-user.png", customer.AvatarUrl);
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
        public ZaloAuthService CreateZaloAuthService() => new(
            Context,
            Configuration,
            new FakeZaloGraphApiClient());

        public Task<ZaloLoginResponseDto> LoginCustomerAsync(
            ZaloAuthService zaloAuthService,
            Guid brandId,
            string zaloUserId,
            string fullName)
        {
            FakeZaloGraphApiClient.SetProfile(zaloUserId, fullName);
            return zaloAuthService.LoginAsync(new ZaloLoginRequestDto
            {
                BrandId = brandId,
                AccessToken = zaloUserId
            });
        }

        private sealed class FakeZaloGraphApiClient : IZaloGraphApiClient
        {
            private static readonly Dictionary<string, ZaloProfileDto> Profiles = new();

            public static void SetProfile(string accessToken, string fullName)
            {
                Profiles[accessToken] = new ZaloProfileDto
                {
                    Id = accessToken,
                    Name = fullName,
                    Picture = new ZaloPictureDto
                    {
                        Data = new ZaloPictureDataDto
                        {
                            Url = $"https://example.com/{accessToken}.png"
                        }
                    }
                };
            }

            public Task<ZaloProfileDto> GetProfileAsync(string accessToken, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(Profiles[accessToken]);
            }
        }

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
