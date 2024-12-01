using Avalonia.Media.Imaging;
using Avalonia.Platform;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.CollectionDownload;

public class CollectionDownloadDesignViewModel : APageViewModel<ICollectionDownloadViewModel>, ICollectionDownloadViewModel
{
    public CollectionDownloadTreeDataGridAdapter TreeDataGridAdapter { get; } = null!;

    public CollectionDownloadDesignViewModel() : base(new DesignWindowManager()) { }

    public string Name => "Vanilla+ [Quality of Life]";
    public CollectionSlug Slug { get; } = CollectionSlug.From("tckf0m");
    public RevisionNumber RevisionNumber { get; } = RevisionNumber.From(6);
    public string AuthorName => "Lowtonotolerance";

    public string Summary =>
        "1.6.14 The story of Stardew Valley expands outside of Pelican Town with this expanded collection designed to stay true to the original game. Created with co-op in mind, perfect for experienced solo-players. Easy install, includes configuration.";

    public string Category => "Themed";
    public bool IsAdult => true;
    public int RequiredDownloadsCount => 9;
    public int OptionalDownloadsCount => 2;
    public Bitmap AuthorAvatar => new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/avatar.webp")));
    public ulong EndorsementCount => 248;
    public ulong TotalDownloads => 30_000;
    public Size TotalSize { get; } = Size.From(76_123_456);
    public Percent OverallRating { get; } = Percent.CreateClamped(0.82);
    public Bitmap TileImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/collection_tile_image.png")));
    public Bitmap BackgroundImage { get; } = new(AssetLoader.Open(new Uri("avares://NexusMods.App.UI/Assets/DesignTime/header-background.webp")));
    public string CollectionStatusText { get; } = "0 of 9 mods downloaded";

    public ReactiveCommand<Unit> DownloadAllCommand { get; } = new ReactiveCommand();
    public ReactiveCommand<Unit> InstallCollectionCommand { get; } = new ReactiveCommand();
}
