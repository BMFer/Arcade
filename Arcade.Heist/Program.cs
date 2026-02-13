using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Arcade.Core.AI;
using Arcade.Core.Configuration;
using Arcade.Core.Data;
using Arcade.Heist.AI;
using Arcade.Heist.Configuration;
using Arcade.Heist.Data;
using Arcade.Heist.Discord;
using Arcade.Heist.Discord.Handlers;
using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Words;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        // Configuration — split across Bot, AI, Heist sections
        services.Configure<BotOptions>(context.Configuration.GetSection(BotOptions.SectionName));
        services.Configure<AiOptions>(context.Configuration.GetSection(AiOptions.SectionName));
        services.Configure<HeistOptions>(context.Configuration.GetSection(HeistOptions.SectionName));

        // Override token from env var if present
        var envToken = Environment.GetEnvironmentVariable("HEIST_BOT_TOKEN");
        if (!string.IsNullOrEmpty(envToken))
        {
            services.PostConfigure<BotOptions>(options => options.BotToken = envToken);
        }

        var socketConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds
                | GatewayIntents.GuildMessages
                | GatewayIntents.MessageContent
                | GatewayIntents.GuildMembers,
            AlwaysDownloadUsers = true
        };

        services.AddSingleton(socketConfig);
        services.AddSingleton<DiscordSocketClient>(sp => new DiscordSocketClient(sp.GetRequiredService<DiscordSocketConfig>()));
        services.AddSingleton<InteractionService>(sp => new InteractionService(sp.GetRequiredService<DiscordSocketClient>()));

        // Game services
        services.AddSingleton<WordBank>();
        services.AddSingleton<WordScrambler>();
        services.AddSingleton<TowerManager>();
        services.AddSingleton<PlayerManager>();
        services.AddSingleton<PuzzleManager>();
        services.AddSingleton<CardManager>();

        // AI & data services
        services.AddSingleton<TowerDataStore>();
        services.AddSingleton<PlayerDataStore>();
        services.AddHttpClient<OllamaService>();
        services.AddSingleton<IAssistantEventHandler, HeistAssistantEventHandler>();
        services.AddSingleton<AssistantService>();

        // GameManager depends on AssistantService
        services.AddSingleton<GameManager>();

        // Discord services
        services.AddSingleton<MessageHandler>();
        services.AddHostedService<HeistBotHostedService>();
    })
    .Build();

await host.RunAsync();
