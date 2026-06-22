using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;

namespace LibraHub.Library.Application.Resilience;

public static class TransientErrorDetector
{
    private static readonly string[] TransientPostgresSqlStates =
        { "40001", "40P01", "08000", "08003", "08006", "08007", "57P01", "57P02", "57P03" };

    public static bool IsTransientPostgresError(NpgsqlException ex)
        => ex.SqlState is { } sqlState && TransientPostgresSqlStates.Contains(sqlState);

    public static bool IsTransientHttpError(HttpRequestException ex)
        => ex.InnerException is WebException webEx &&
           (webEx.Status == WebExceptionStatus.Timeout ||
            webEx.Status == WebExceptionStatus.ConnectFailure ||
            webEx.Status == WebExceptionStatus.ReceiveFailure);

    public static bool IsRetryable(Exception ex)
        => ex is NpgsqlException npgsqlEx && IsTransientPostgresError(npgsqlEx) ||
           ex is DbUpdateException dbEx && dbEx.InnerException is NpgsqlException innerNpgsqlEx && IsTransientPostgresError(innerNpgsqlEx) ||
           ex is HttpRequestException httpEx && IsTransientHttpError(httpEx) ||
           ex is TimeoutException;
}
