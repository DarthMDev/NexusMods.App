using DynamicData.Kernel;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi;

[PublicAPI]
public class NexusModsLibrary
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly INexusApiClient _apiClient;

    /// <summary>
    /// Constructor.
    /// </summary>
    public NexusModsLibrary(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _apiClient = serviceProvider.GetRequiredService<INexusApiClient>();
    }

    public async Task<NexusModsModPageMetadata.ReadOnly> GetOrAddModPage(
        ModId modId,
        GameDomain gameDomain,
        CancellationToken cancellationToken = default)
    {
        var modPageEntities = NexusModsModPageMetadata.FindByModId(_connection.Db, modId);
        if (modPageEntities.TryGetFirst(x => x.GameDomain == gameDomain, out var modPage)) return modPage;

        using var tx = _connection.BeginTransaction();

        var modInfo = await _apiClient.ModInfoAsync(gameDomain.ToString(), modId, cancellationToken);

        var newModPage = new NexusModsModPageMetadata.New(tx)
        {
            Name = modInfo.Data.Name,
            ModId = modId,
            GameDomain = gameDomain,
        };

        var txResults = await tx.Commit();
        return txResults.Remap(newModPage);
    }

    public async Task<NexusModsFileMetadata.ReadOnly> GetOrAddFile(
        FileId fileId,
        NexusModsModPageMetadata.ReadOnly modPage,
        GameDomain gameDomain,
        CancellationToken cancellationToken = default)
    {
        var fileEntities = NexusModsFileMetadata.FindByFileId(_connection.Db, fileId);
        if (fileEntities.TryGetFirst(x => x.ModPageId == modPage, out var file)) return file;

        using var tx = _connection.BeginTransaction();

        var filesResponse = await _apiClient.ModFilesAsync(gameDomain.ToString(), modPage.ModId, cancellationToken);
        var files = filesResponse.Data.Files;

        if (!files.TryGetFirst(x => x.FileId == fileId, out var fileInfo))
            throw new NotImplementedException();

        var newFile = new NexusModsFileMetadata.New(tx)
        {
            Name = fileInfo.Name,
            Version = fileInfo.Version,
            FileId = fileId,
            ModPageId = modPage,
        };

        var txResults = await tx.Commit();
        return txResults.Remap(newFile);
    }

    public async Task<Uri> GetDownloadUri(
        NexusModsFileMetadata.ReadOnly file,
        Optional<(NXMKey, DateTime)> nxmData,
        CancellationToken cancellationToken = default)
    {
        Response<DownloadLink[]> links;

        if (nxmData.HasValue)
        {
            // NOTE(erri120): the key and expiration date are required for free users to be able to download anything
            var (key, expirationDate) = nxmData.Value;
            links = await _apiClient.DownloadLinksAsync(
                file.ModPage.GameDomain.ToString(),
                file.ModPage.ModId,
                file.FileId,
                key: key,
                expireTime: expirationDate,
                token: cancellationToken
            );
        }
        else
        {
            // NOTE(erri120): premium-only API
            links = await _apiClient.DownloadLinksAsync(
                file.ModPage.GameDomain.ToString(),
                file.ModPage.ModId,
                file.FileId,
                token: cancellationToken
            );
        }

        // NOTE(erri120): The first download link is the preferred download location as
        // set by the user in their settings. By default, this will be the CDN, which
        // is going to be the fastest location 99% of the time.
        return links.Data.First().Uri;
    }

    public async Task<NexusModsDownloadJob> CreateDownloadJob(
        AbsolutePath destination,
        NXMModUrl url,
        CancellationToken cancellationToken)
    {
        var nxmData = url.Key is not null && url.ExpireTime is not null ? (url.Key.Value, url.ExpireTime.Value) : Optional.None<(NXMKey, DateTime)>();
        var modPage = await GetOrAddModPage(url.ModId, GameDomain.From(url.Game), cancellationToken);
        var file = await GetOrAddFile(url.FileId, modPage, GameDomain.From(url.Game), cancellationToken);
        return await CreateDownloadJob(destination, file, nxmData, cancellationToken);
    }

    public async Task<NexusModsDownloadJob> CreateDownloadJob(
        AbsolutePath destination,
        NexusModsFileMetadata.ReadOnly file,
        Optional<(NXMKey, DateTime)> nxmData,
        CancellationToken cancellationToken)
    {

        var uri = await GetDownloadUri(file, nxmData, cancellationToken: cancellationToken);

        var worker = _serviceProvider.GetRequiredService<NexusModsDownloadJobWorker>();

        using var tx = _connection.BeginTransaction();
        var newState = new NexusModsDownloadJobPersistedState.New(tx, out var id)
        {
            FileMetadataId = file,
            HttpDownloadJobPersistedState = new HttpDownloadJobPersistedState.New(tx, id)
            {
                Destination = destination,
                Uri = uri,
                DownloadPageUri = file.ModPage.GetUri(),
                PersistedJobState = new PersistedJobState.New(tx, id)
                {
                    Status = JobStatus.None,
                    Worker = worker,
                },
            },
        };

        var txResult = await tx.Commit();
        var state = txResult.Remap(newState);
        
        var monitor = _serviceProvider.GetRequiredService<IJobMonitor>();

        var job = new NexusModsDownloadJob(_connection, state, worker: worker, monitor: monitor)
        {
            HttpDownloadJob = new HttpDownloadJob(_connection, state.AsHttpDownloadJobPersistedState(), 
                worker: _serviceProvider.GetRequiredService<HttpDownloadJobWorker>(),
                monitor: monitor),
        };

        return job;
    }
}