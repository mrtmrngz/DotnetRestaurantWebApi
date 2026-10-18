namespace RestaurantApi.IntegrationTests.Setup;

// Testlerde zamanı dondurup deterministik test yazabilmek için kullanılan saat.
// Varsayılan olarak gerçek UTC zamanını döner; SetUtcNow ile dondurulur, Reset ile normale döner.
public class TestTimeProvider: TimeProvider
{
    private DateTimeOffset? _utcNow;

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow ?? DateTimeOffset.UtcNow;
    }

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public void Reset()
    {
        _utcNow = null;
    }
}
