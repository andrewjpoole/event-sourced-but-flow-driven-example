using Moq;
using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Tests.e2eComponentTests.Framework.Persistence;

public class FakeDbTransactionWrapped : IDbTransactionWrapped
{
    public bool WasCommitted { get; private set; } = false;
    public bool WasRolledBack { get; private set; } = false;

    public void Commit()
    {
        WasCommitted = true;
    }

    public IRetryableConnection GetConnection()
    {
        return new Mock<IRetryableConnection>().Object;
    }

    public void Rollback()
    {
        WasRolledBack = true;
    }

    public System.Data.IDbTransaction ToIDbTransaction()
    {
        return new Mock<System.Data.IDbTransaction>().Object;
    }
}