namespace Bot.Services;

public class BotBackgroundService : BackgroundService
{
    private readonly BotService _telegramBotService;

    public BotBackgroundService(BotService telegramBotService)
    {
        _telegramBotService = telegramBotService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _telegramBotService.StartAsync(stoppingToken);
    }
}