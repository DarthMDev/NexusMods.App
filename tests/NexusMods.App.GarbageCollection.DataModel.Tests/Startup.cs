using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
namespace NexusMods.App.GarbageCollection.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDefaultServicesForTesting()
            .AddStandardGameLocators(false)
            .AddLoadoutAbstractions()
            .AddStubbedGameLocators();
    }
}
