﻿using WeatherApp.Infrastructure.RetryableDapperConnection;

namespace WeatherApp.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    IRetryableConnection Create();
}