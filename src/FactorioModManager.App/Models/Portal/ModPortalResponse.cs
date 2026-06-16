using System.Text.Json;
using System.Text.Json.Serialization;

namespace FactorioModManager.App.Models.Portal;

// The search endpoint returns latest_release as a string ID; the browse endpoint returns it as an object.
// This converter silently returns null for the string case.
internal sealed class PortalModReleaseConverter : JsonConverter<PortalModRelease?>
{
    public override PortalModRelease? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.StartObject => JsonSerializer.Deserialize<PortalModRelease>(ref reader, options),
            JsonTokenType.String => null, // string ID — consume token and ignore
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Unexpected token {reader.TokenType} for latest_release")
        };
    }

    public override void Write(Utf8JsonWriter writer, PortalModRelease? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else JsonSerializer.Serialize(writer, value, options);
    }
}

// The search endpoint returns tags as an array of strings; the browse endpoint returns tag objects.
// This converter normalises both into List<PortalModTag>.
internal sealed class PortalModTagListConverter : JsonConverter<List<PortalModTag>?>
{
    public override List<PortalModTag>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException($"Unexpected token {reader.TokenType} for tags");

        var list = new List<PortalModTag>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
                list.Add(new PortalModTag { Name = reader.GetString() });
            else if (reader.TokenType == JsonTokenType.StartObject)
                list.Add(JsonSerializer.Deserialize<PortalModTag>(ref reader, options)!);
        }
        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<PortalModTag>? value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}

public sealed class ModPortalResponse
{
    [JsonPropertyName("results")]
    public List<PortalModResult>? Results { get; set; }

    [JsonPropertyName("pagination")]
    public PortalPagination? Pagination { get; set; }
}

public sealed class PortalPagination
{
    [JsonPropertyName("page")]
    public int? Page { get; set; }

    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    [JsonPropertyName("page_count")]
    public int? PageCount { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }

    [JsonPropertyName("links")]
    public PortalPaginationLinks? Links { get; set; }
}

public sealed class PortalPaginationLinks
{
    [JsonPropertyName("first")]
    public string? First { get; set; }

    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("last")]
    public string? Last { get; set; }
}

public sealed class PortalModResult
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("owner")]
    public string? Owner { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("downloads_count")]
    public int? DownloadsCount { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("score")]
    public double? Score { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("latest_release")]
    [JsonConverter(typeof(PortalModReleaseConverter))]
    public PortalModRelease? LatestRelease { get; set; }

    // Search endpoint provides version as a flat string instead of inside latest_release
    [JsonPropertyName("latest_release_version")]
    public string? LatestReleaseVersion { get; set; }

    // Search endpoint provides compatible Factorio versions as a flat array
    [JsonPropertyName("factorio_versions")]
    public List<string>? FactorioVersions { get; set; }

    [JsonPropertyName("releases")]
    public List<PortalModRelease>? Releases { get; set; }

    [JsonPropertyName("tags")]
    [JsonConverter(typeof(PortalModTagListConverter))]
    public List<PortalModTag>? Tags { get; set; }

    [JsonPropertyName("source_url")]
    public string? SourceUrl { get; set; }

    [JsonPropertyName("homepage")]
    public string? Homepage { get; set; }

    [JsonPropertyName("changelog")]
    public string? Changelog { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}

public sealed class PortalModRelease
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("released_at")]
    public string? ReleasedAt { get; set; }

    [JsonPropertyName("info_json")]
    public PortalReleaseInfoJson? InfoJson { get; set; }

    [JsonPropertyName("download_url")]
    public string? DownloadUrl { get; set; }

    [JsonPropertyName("file_name")]
    public string? FileName { get; set; }

    [JsonPropertyName("sha1")]
    public string? Sha1 { get; set; }
}

public sealed class PortalReleaseInfoJson
{
    [JsonPropertyName("factorio_version")]
    public string? FactorioVersion { get; set; }

    [JsonPropertyName("dependencies")]
    public List<string>? Dependencies { get; set; }
}

public sealed class PortalModTag
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("id")]
    public int? Id { get; set; }
}
