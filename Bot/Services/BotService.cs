using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace Bot.Services;

public class BotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotService> _logger;
    private readonly string _channelId;

    public BotService(string botToken, ILogger<BotService> logger)
    {
        _botClient = new TelegramBotClient(botToken);
        _channelId = "@hasanboevs_blog"; // Ensure channel ID starts with '@'
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
        };

        _botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken
        );

        var me = await _botClient.GetMeAsync();
        Console.WriteLine($"Start listening for @{me.Username}");
        _logger.LogInformation($"Start listening for @{me.Username}");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message!.Type == MessageType.Text)
        {
            var message = update.Message;

            if (message.Text!.ToLower() == "/start")
            {
                await HandleStartCommandAsync(botClient, message, cancellationToken);
            }
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            var callbackQuery = update.CallbackQuery;
            if (callbackQuery!.Data == "check_membership")
            {
                await HandleCheckMembershipCallbackAsync(botClient, callbackQuery, cancellationToken);
            }
        }
    }

    private async Task HandleStartCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var isMember = await IsUserMemberOfChannel(message.Chat.Id, cancellationToken);
        if (!isMember)
        {
            _logger.LogInformation($"Siz {message.Chat.Id} bu kanalni foydalanuvhisi emassiz {_channelId}. Sending join message.");
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Bu botdan foydalanish va uni funksiyalarini ishlatish uchun quyidagi tugamani bosish orqali kanalga obuna bo'lishingniz kerak",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithUrl("Join Channel", $"https://t.me/{_channelId.TrimStart('@')}"),
                    InlineKeyboardButton.WithCallbackData("Check Membership", "check_membership")
                }),
                cancellationToken: cancellationToken
            );
        }
        else
        {
            _logger.LogInformation($"User {message.Chat.Id} is already a member of the channel {_channelId}. Sending welcome message.");
            await SendWelcomeMessageAsync(botClient, message.Chat.Id, cancellationToken);
        }
    }

    private async Task HandleCheckMembershipCallbackAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var isMember = await IsUserMemberOfChannel(callbackQuery.From.Id, cancellationToken);
        if (!isMember)
        {
            _logger.LogInformation($"User {callbackQuery.From.Id} checked membership and is not a member.");
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "You are not yet a member of the channel. Please join the channel and try again.",
                cancellationToken: cancellationToken
            );
        }
        else
        {
            _logger.LogInformation($"User {callbackQuery.From.Id} checked membership and is a member.");
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Thank you for joining the channel! You can now use the bot.",
                cancellationToken: cancellationToken
            );

        }
    }

    private async Task SendWelcomeMessageAsync(ITelegramBotClient botClient, long chatId, CancellationToken cancellationToken)
    {
        var photoUrl = "https://i.pinimg.com/736x/51/db/01/51db01c5b07c3cf5c3e2955a418bfae8.jpg";
        _logger.LogInformation($"Sending photo with URL: {photoUrl}");

        try
        {
            var replyKeyboardMarkup = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithWebApp("YouTube", new WebAppInfo() { Url = "https://www.youtube.com/" }),
            });

            await botClient.SendPhotoAsync(
                chatId: chatId,
                photo: new InputFileUrl(photoUrl),
                caption: "Welcome to, YouTube",
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending photo: {ex.Message}");
            throw;
        }
    }

    private async Task<bool> IsUserMemberOfChannel(long userId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Checking membership status for user {userId} in channel {_channelId}.");
            var chatMember = await _botClient.GetChatMemberAsync(new ChatId(_channelId), userId, cancellationToken);
            bool isMember = chatMember.Status == ChatMemberStatus.Member || chatMember.Status == ChatMemberStatus.Administrator || chatMember.Status == ChatMemberStatus.Creator;
            _logger.LogInformation($"Membership status for user {userId} in channel {_channelId}: {isMember}");
            return isMember;
        }
        catch (ApiRequestException ex) when (ex.ErrorCode == 400)
        {
            _logger.LogWarning($"User {userId} is not a member of channel {_channelId}.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error checking membership status for user {userId}: {ex.Message}");
            throw;
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(errorMessage);
        _logger.LogError(errorMessage);
        return Task.CompletedTask;
    }
}