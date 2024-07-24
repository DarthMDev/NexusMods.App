using FluentAssertions;
using NexusMods.App.GarbageCollection.Errors;
using NexusMods.App.GarbageCollection.Structs;
using NexusMods.App.GarbageCollection.Tests.Helpers;
using NexusMods.Paths;
using NexusMods.Paths.TestingHelpers;
namespace NexusMods.App.GarbageCollection.Tests;

public class ArchiveGarbageCollectorTests
{
    [Theory, AutoFileSystem]
    public void AddFiles_ShouldAddAllHashes(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState = new MockParsedHeaderState(hash1, hash2);

        // Act
        collector.AddArchive(archivePath, headerState);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);

        // Assert

        // Contains the hashes
        collector.HashToArchive[hash1].FilePath.Should().Be(archivePath);
        collector.HashToArchive[hash2].FilePath.Should().Be(archivePath);

        // The inner archive 
        collector.HashToArchive[hash1].Entries.Should().ContainKey(hash1);
        collector.HashToArchive[hash1].Entries.Should().ContainKey(hash2);

        // Check the ref count
        collector.HashToArchive[hash1].Entries[hash1].GetRefCount().Should().Be(1);
        collector.HashToArchive[hash2].Entries[hash2].GetRefCount().Should().Be(1);
    }
    
    [Theory, AutoFileSystem]
    public void AddFiles_WithMultipleReferences_ShouldIncreaseRefCount(AbsolutePath archivePath)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState = new MockParsedHeaderState(hash1, hash2);

        // Act
        collector.AddArchive(archivePath, headerState);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);

        // Assert

        // Check the ref count
        collector.HashToArchive[hash1].Entries[hash1].GetRefCount().Should().Be(2);
        collector.HashToArchive[hash2].Entries[hash2].GetRefCount().Should().Be(2);
    }

    [Fact]
    public void AddReferencedFile_ShouldThrowForUnknownHash()
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState>();
        var unknownHash = (Hash)999;

        // Act & Assert
        var act = () => collector.AddReferencedFile(unknownHash);
        act.Should().Throw<UnknownFileException>();
    }

    [Theory, AutoFileSystem]
    public void AddArchive_ShouldHandleMultipleArchives(AbsolutePath path1, AbsolutePath path2)
    {
        // Arrange
        var collector = new ArchiveGarbageCollector<MockParsedHeaderState>();
        var hash1 = (Hash)1;
        var hash2 = (Hash)2;
        var headerState1 = new MockParsedHeaderState(hash1);
        var headerState2 = new MockParsedHeaderState(hash2);

        // Act
        collector.AddArchive(path1, headerState1);
        collector.AddArchive(path2, headerState2);
        collector.AddReferencedFile(hash1);
        collector.AddReferencedFile(hash2);

        // Assert
        // Verify that each hash is associated with the correct archive path
        collector.HashToArchive[hash1].FilePath.Should().Be(path1);
        collector.HashToArchive[hash2].FilePath.Should().Be(path2);

        // Verify that each hash has the correct reference count
        collector.HashToArchive[hash1].Entries[hash1].GetRefCount().Should().Be(1);
        collector.HashToArchive[hash2].Entries[hash2].GetRefCount().Should().Be(1);

        // Verify that each archive has the correct HeaderState
        collector.HashToArchive[hash1].HeaderState.Should().BeSameAs(headerState1);
        collector.HashToArchive[hash2].HeaderState.Should().BeSameAs(headerState2);

        // Verify that each archive only contains its own hash
        collector.HashToArchive[hash1].Entries.Should().NotContainKey(hash2);
        collector.HashToArchive[hash2].Entries.Should().NotContainKey(hash1);
    }
}
