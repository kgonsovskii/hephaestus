using Microsoft.Extensions.Logging;

namespace Commons;

public static class LoggerExceptionExtensions
{
    public static void LogErrorMessage(this ILogger logger, Exception ex, string message, params object?[] args)
    {
        Emit(logger, LogLevel.Error, ex, message, args);
    }

    public static void LogWarningMessage(this ILogger logger, Exception ex, string message, params object?[] args)
    {
        Emit(logger, LogLevel.Warning, ex, message, args);
    }

    public static string RootMessage(Exception ex)
    {
        var root = ex;
        while (root.InnerException != null)
            root = root.InnerException;
        return root.Message;
    }

    private static void Emit(ILogger logger, LogLevel level, Exception ex, string message, object?[] args)
    {
        var merged = new object?[args.Length + 1];
        if (args.Length > 0)
            Array.Copy(args, merged, args.Length);
        merged[args.Length] = RootMessage(ex);
        logger.Log(level, message + " Exception={Exception}", merged);
    }
}
