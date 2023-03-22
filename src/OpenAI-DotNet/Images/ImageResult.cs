﻿using System.Text.Json.Serialization;

namespace OpenAI.Images
{
    internal sealed class ImageResult
    {
        [JsonInclude]
        [JsonPropertyName("url")]
        public string Url { get; private set; }
    }
}
