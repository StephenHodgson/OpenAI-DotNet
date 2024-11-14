﻿// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace OpenAI.Realtime
{
    public sealed class ResponseAudioResponse : BaseRealtimeEvent, IServerEvent, IRealtimeEventStream
    {
        /// <inheritdoc />
        [JsonInclude]
        [JsonPropertyName("event_id")]
        public override string EventId { get; internal set; }

        /// <inheritdoc />
        [JsonInclude]
        [JsonPropertyName("type")]
        public override string Type { get; protected set; }

        /// <summary>
        /// The ID of the response.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("response_id")]
        public string ResponseId { get; private set; }

        /// <summary>
        /// The ID of the item.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("item_id")]
        public string ItemId { get; private set; }

        /// <summary>
        /// The index of the output item in the response.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("output_index")]
        public string OutputIndex { get; private set; }

        /// <summary>
        /// The index of the content part in the item's content array.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("content_index")]
        public string ContentIndex { get; private set; }

        [JsonInclude]
        [JsonPropertyName("delta")]
        public string Delta { get; private set; }

        [JsonIgnore]
        public bool IsDelta => Type.EndsWith("delta");

        [JsonIgnore]
        public bool IsDone => Type.EndsWith("done");
    }
}
