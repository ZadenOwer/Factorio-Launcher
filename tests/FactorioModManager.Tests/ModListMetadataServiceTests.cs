using FactorioModManager.App.Factorio;

namespace FactorioModManager.Tests;

public sealed class ModListMetadataServiceTests
{
    [Fact]
    public void Load_returns_empty_metadata_when_sidecar_is_missing()
    {
        using var temp = new TempDirectory();

        var metadata = new ModListMetadataService().Load(temp.Path);

        Assert.Equal(string.Empty, metadata.Description);
        Assert.Empty(metadata.SelectedVersions);
    }

    [Fact]
    public void Load_returns_empty_metadata_when_sidecar_is_invalid()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(ModListMetadataService.GetMetadataPath(temp.Path), "not-json");

        var metadata = new ModListMetadataService().Load(temp.Path);

        Assert.Equal(string.Empty, metadata.Description);
        Assert.Empty(metadata.SelectedVersions);
    }

    [Fact]
    public void Save_and_load_round_trips_description_versions_and_activation_time()
    {
        using var temp = new TempDirectory();
        var service = new ModListMetadataService();
        var lastActivated = DateTimeOffset.UtcNow.AddHours(-2);

        service.Save(
            temp.Path,
            "Space overhaul",
            new Dictionary<string, string> { ["space-exploration"] = "0.6.128" },
            null,
            lastActivated);

        var metadata = service.Load(temp.Path);

        Assert.Equal("Space overhaul", metadata.Description);
        Assert.Equal("0.6.128", metadata.SelectedVersions["space-exploration"]);
        Assert.NotNull(metadata.CreatedUtc);
        Assert.NotNull(metadata.UpdatedUtc);
        Assert.Equal(lastActivated.ToUnixTimeSeconds(), metadata.LastActivatedUtc!.Value.ToUnixTimeSeconds());
    }

    [Fact]
    public void RecordActivation_preserves_existing_metadata()
    {
        using var temp = new TempDirectory();
        var service = new ModListMetadataService();
        service.Save(temp.Path, "Description", new Dictionary<string, string> { ["mod-a"] = "1.0.0" }, null, null);

        service.RecordActivation(temp.Path);

        var metadata = service.Load(temp.Path);
        Assert.Equal("Description", metadata.Description);
        Assert.Equal("1.0.0", metadata.SelectedVersions["mod-a"]);
        Assert.NotNull(metadata.LastActivatedUtc);
    }
}
