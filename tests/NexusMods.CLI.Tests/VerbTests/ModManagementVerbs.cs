using FluentAssertions;
using NexusMods.Paths;
using NexusMods.ProxyConsole.Abstractions.Implementations;
using NexusMods.StandardGameLocators.TestHelpers.StubbedGames;

namespace NexusMods.CLI.Tests.VerbTests;

public class ModManagementVerbs(StubbedGame stubbedGame, IServiceProvider provider) : AVerbTest(provider)
{
    [Fact]
    public async Task CanCreateAndManageLists()
    {
        var listName = Guid.NewGuid().ToString();

        var log = await Run("manage-game", "-g", "stubbed-game", "-v",
            stubbedGame.Installations.First().Version.ToString(), "-n", listName);

        log = await Run("list-loadouts");

        log.LastTableColumns.Should().BeEquivalentTo("Name", "Game", "Id", "Mod Count");
        log.TableCellsWith(0, listName).Should().NotBeEmpty();

        log = await Run("list-mods", "-l", listName);
        log.LastTable.Rows.Length.Should().Be(1);

        log = await Run("install-mod", "-l", listName, "-f", Data7ZipLZMA2.ToString(), "-n", Data7ZipLZMA2.GetFileNameWithoutExtension());

        log = await Run("list-mods", "-l", listName);
        log.LastTable.Rows.Length.Should().Be(2);

        log = await Run("list-mod-contents", "-l", listName, "-m", Data7ZipLZMA2.GetFileNameWithoutExtension());
        log.LastTable.Rows.Length.Should().Be(3);

        log = await Run("flatten-loadout", "-l", listName);
        log.LastTable.Rows.Length.Should().Be(7);

        log = await Run("apply", "-l", listName);
        log.Last<Text>().Template.Should().Contain($"Applied {listName}");
    }
}
