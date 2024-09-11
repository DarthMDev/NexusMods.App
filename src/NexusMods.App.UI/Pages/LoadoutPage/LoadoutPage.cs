using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization.Attributes;
using NexusMods.Abstractions.Settings;
using NexusMods.App.UI.Resources;
using NexusMods.App.UI.Settings;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Icons;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.App.UI.Pages.LoadoutPage;

[JsonName("NexusMods.App.UI.Pages.Library.LoadoutPageContext")]
public record LoadoutPageContext : IPageFactoryContext
{
    public required LoadoutId LoadoutId { get; init; }
}

[UsedImplicitly]
public class LoadoutPageFactory : APageFactory<ILoadoutViewModel, LoadoutPageContext>
{
    private readonly ISettingsManager _settingsManager;
    private readonly IConnection _connection;

    public LoadoutPageFactory(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _settingsManager = serviceProvider.GetRequiredService<ISettingsManager>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
    }

    public static readonly PageFactoryId StaticId = PageFactoryId.From(Guid.Parse("62fda6ce-e6b7-45d6-936f-a8f325bfc644"));
    public override PageFactoryId Id => StaticId;

    public override ILoadoutViewModel CreateViewModel(LoadoutPageContext context)
    {
        // Default to the user group for now
        var userGroup = Loadout.Load(_connection.Db, context.LoadoutId).MutableCollections().First().AsLoadoutItemGroup();
        var vm = new LoadoutViewModel(ServiceProvider.GetRequiredService<IWindowManager>(), ServiceProvider, context.LoadoutId, userGroup.LoadoutItemGroupId);
        return vm;
    }

    public override IEnumerable<PageDiscoveryDetails?> GetDiscoveryDetails(IWorkspaceContext workspaceContext)
    {
        if (workspaceContext is not LoadoutContext loadoutContext) yield break;

        yield return new PageDiscoveryDetails
        {
            SectionName = "Mods",
            ItemName = Language.LoadoutViewPageTitle,
            Icon = IconValues.Collections,
            PageData = new PageData
            {
                FactoryId = Id,
                Context = new LoadoutPageContext
                {
                    LoadoutId = loadoutContext.LoadoutId,
                },
            },
        };
    }
}
