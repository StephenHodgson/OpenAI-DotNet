﻿// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;

namespace OpenAI.Realtime
{
    public sealed class InputAudioBufferStartedResponse : BaseRealtimeEvent, IServerEvent
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
        /// Milliseconds since the session started when speech was detected.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("audio_start_ms")]
        public int AudioStartMs { get; private set; }

        /// <summary>
        /// The ID of the user message item that will be created when speech stops.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("item_id")]
        public string ItemId { get; private set; }
    }
}
