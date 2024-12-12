namespace _20strike;

public class Worker(ILogger<Worker> logger) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows()) return;
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now); // I don't understand this
        Environment.CurrentDirectory = AppContext.BaseDirectory;
        try
        {
            Application app = new()
            {
                cancellationToken = stoppingToken
            };
            stoppingToken.Register(app.Stop);
            await app.Start(); // It does need to be async to not to hang app
        }
        catch (Exception e)
        {
            _logger.LogInformation(e.Message);
            Environment.Exit(2);
        }
    }
}
