using OrderSphere.Webhooks.Domain.Enums;

namespace OrderSphere.Webhooks.Tests.Domain;

public sealed class WebhookDeliveryTests
{
    private static WebhookDelivery CreateDelivery() =>
        new(WebhookSubscriptionId.New(), "OrderPlaced", Guid.NewGuid(), """{"orderId":"test"}""");

    // ── Construction ────────────────────────────────────────────────────────────

    [Fact]
    public void Constructor_SetsPendingStatus()
    {
        var delivery = CreateDelivery();

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
    }

    // ── RecordSuccess ───────────────────────────────────────────────────────────

    [Fact]
    public void RecordSuccess_SetsStatusSucceeded()
    {
        var delivery = CreateDelivery();

        delivery.RecordSuccess(200);

        delivery.Status.Should().Be(DeliveryStatus.Succeeded);
    }

    [Fact]
    public void RecordSuccess_IncrementsAttemptCount()
    {
        var delivery = CreateDelivery();

        delivery.RecordSuccess(200);

        delivery.AttemptCount.Should().Be(1);
    }

    [Fact]
    public void RecordSuccess_SetsHttpStatusAndClearsRetry()
    {
        var delivery = CreateDelivery();

        delivery.RecordSuccess(201);

        delivery.LastHttpStatus.Should().Be(201);
        delivery.LastError.Should().BeNull();
        delivery.NextRetryAt.Should().BeNull();
    }

    // ── RecordFailure — below max attempts ──────────────────────────────────────

    [Fact]
    public void RecordFailure_Attempt1_SetsRetryWithBackoff10s()
    {
        var delivery = CreateDelivery();
        var before = DateTime.UtcNow;

        delivery.RecordFailure(500, "Internal Server Error");

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(1);
        delivery.NextRetryAt.Should().NotBeNull();
        // Backoff = 10 * 1^2 = 10 s
        delivery.NextRetryAt!.Value.Should().BeCloseTo(before.AddSeconds(10), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_Attempt2_SetsRetryWithBackoff40s()
    {
        var delivery = CreateDelivery();
        delivery.RecordFailure(503, "Service Unavailable");
        var before = DateTime.UtcNow;

        delivery.RecordFailure(503, "Service Unavailable");

        // Backoff = 10 * 2^2 = 40 s
        delivery.NextRetryAt!.Value.Should().BeCloseTo(before.AddSeconds(40), TimeSpan.FromSeconds(2));
        delivery.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void RecordFailure_Attempt3_SetsRetryWithBackoff90s()
    {
        var delivery = CreateDelivery();
        delivery.RecordFailure(500, "err");
        delivery.RecordFailure(500, "err");
        var before = DateTime.UtcNow;

        delivery.RecordFailure(500, "err");

        // Backoff = 10 * 3^2 = 90 s
        delivery.NextRetryAt!.Value.Should().BeCloseTo(before.AddSeconds(90), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_Attempt4_SetsRetryWithBackoff160s()
    {
        var delivery = CreateDelivery();
        delivery.RecordFailure(500, "err");
        delivery.RecordFailure(500, "err");
        delivery.RecordFailure(500, "err");
        var before = DateTime.UtcNow;

        delivery.RecordFailure(500, "err");

        // Backoff = 10 * 4^2 = 160 s
        delivery.NextRetryAt!.Value.Should().BeCloseTo(before.AddSeconds(160), TimeSpan.FromSeconds(2));
        delivery.Status.Should().Be(DeliveryStatus.Pending);
    }

    // ── RecordFailure — at max attempts (5) ─────────────────────────────────────

    [Fact]
    public void RecordFailure_Attempt5_SetsStatusFailedAndNullsRetry()
    {
        var delivery = CreateDelivery();
        for (int i = 0; i < 4; i++)
            delivery.RecordFailure(500, "err");

        delivery.RecordFailure(500, "final error");

        delivery.Status.Should().Be(DeliveryStatus.Failed);
        delivery.AttemptCount.Should().Be(5);
        delivery.NextRetryAt.Should().BeNull();
    }

    // ── Error message truncation ─────────────────────────────────────────────────

    [Fact]
    public void RecordFailure_ErrorOver1024Chars_TruncatesTo1024()
    {
        var delivery = CreateDelivery();
        var longError = new string('x', 2000);

        delivery.RecordFailure(500, longError);

        delivery.LastError!.Length.Should().Be(1024);
    }

    [Fact]
    public void RecordFailure_ErrorExactly1024Chars_IsNotTruncated()
    {
        var delivery = CreateDelivery();
        var exactError = new string('y', 1024);

        delivery.RecordFailure(500, exactError);

        delivery.LastError!.Length.Should().Be(1024);
    }

    [Fact]
    public void RecordFailure_ErrorUnder1024Chars_IsNotTruncated()
    {
        var delivery = CreateDelivery();
        var shortError = "short error";

        delivery.RecordFailure(500, shortError);

        delivery.LastError.Should().Be(shortError);
    }
}
