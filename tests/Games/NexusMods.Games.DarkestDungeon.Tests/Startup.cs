using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.DarkestDungeon.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<DarkestDungeon>(new Version())
            .AddDarkestDungeon()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddFileStoreAbstractions()
            .AddLoadoutAbstractions()
            .AddSerializationAbstractions()
            .AddInstallerTypes()
            .Validate();
    }
}
