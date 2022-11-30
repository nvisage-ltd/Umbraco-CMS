﻿namespace Umbraco.Cms.Core.Headless;

public class HeadlessMedia
{
    public HeadlessMedia(Guid key, string? name, string mediaType, string url, string? extension, int? width, int? height, IDictionary<string, object?> properties)
    {
        Key = key;
        Name = name;
        MediaType = mediaType;
        Url = url;
        Extension = extension;
        Width = width;
        Height = height;
        Properties = properties;
    }

    public Guid Key { get; }

    public string? Name { get; }

    public string MediaType { get; }

    public string Url { get; }

    public string? Extension { get; }

    public int? Width { get; }

    public int? Height { get; }

    public IDictionary<string, object?> Properties { get; }
}
