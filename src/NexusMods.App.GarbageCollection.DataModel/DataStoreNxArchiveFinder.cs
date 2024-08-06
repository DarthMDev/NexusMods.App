using NexusMods.Abstractions.Settings;
using NexusMods.App.GarbageCollection.Nx;
using NexusMods.Archives.Nx.Headers;
using NexusMods.DataModel;
using NexusMods.Paths;
using NexusMods.Paths.Extensions.Nx.FileProviders;
namespace NexusMods.App.GarbageCollection.DataModel;

/// <summary>
/// This class is responsible for finding all used files within all Nx archives
/// and marking them as 'used'.
/// </summary>
public static class DataStoreNxArchiveFinder
{
    /// <summary>
    /// This method walks through all the archives that the data store can
    /// see, adding their details to the garbage collector.
    /// </summary>
    /// <param name="settingsManager">The settings manager.</param>
    /// <param name="archiveGc">The garbage collector from which to reference count all files.</param>
    public static void FindAllArchives(ISettingsManager settingsManager, ArchiveGarbageCollector<NxParsedHeaderState, FileEntryWrapper> archiveGc)
    {
        var dataModelSettings = settingsManager.Get<DataModelSettings>();
        foreach (var location in dataModelSettings.ArchiveLocations)
        {
            // TODO: Change this to use an in memory FileSystem when NxFileStore supports it.
            var archiveFolderPath = location.ToPath(FileSystem.Shared);
            var allFiles = archiveFolderPath.EnumerateFiles("*.nx").ToArray();
            Parallel.ForEach(allFiles, nxArchivePath =>
            {
                var streamProvider = new FromAbsolutePathProvider { FilePath = nxArchivePath };
                var header = HeaderParser.ParseHeader(streamProvider);
                archiveGc.AddArchive(nxArchivePath, new NxParsedHeaderState(header));
            });
        }
    }
}
