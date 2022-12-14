namespace _20strike;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!OperatingSystem.IsWindows()) return;
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now); // I don't understand this
        Environment.CurrentDirectory = System.AppContext.BaseDirectory;
        try
        {
            Application app = new Application();
            app.cancellationToken = stoppingToken;
            stoppingToken.Register(() => app.stop());
            await app.start(); // It does need to be async to not to hang app
        }
        catch (Exception e)
        {
            _logger.LogInformation(e.Message);
            Environment.Exit(2);
        }
    }
}
