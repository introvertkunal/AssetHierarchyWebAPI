namespace AssetHierarchyWebAPI.Middlewares
{
    public class MissingNameLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly ILogger<MissingNameLoggingMiddleware> _Logger;

        private readonly string _logFilePath = "missing_asset.txt";

        public MissingNameLoggingMiddleware(RequestDelegate next, ILogger<MissingNameLoggingMiddleware> logger)
        {
            _next = next;
            _Logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            bool isLoggingEnabled = endpoint?.Metadata.GetMetadata<LogMissingNameAttribute>() != null;

            if (!isLoggingEnabled)
            {
                await _next(context);
                return;
            }

            string name = context.Request.Query["name"];



            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status404NotFound)
            {
                _Logger.LogWarning($"{System.DateTime.Now} - Asset is not found in the request: {name}");

                //var logEntry = $"{System.DateTime.Now} - Asset not found : {name}{System.Environment.NewLine}";
                //await File.AppendAllTextAsync(_logFilePath, logEntry);

            }


        }
    }
}
