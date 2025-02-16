// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text.Json.Serialization;

namespace OpenAI.Realtime
{
    public sealed class SessionResponse : BaseRealtimeEvent, IServerEvent
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
        /// The session resource configuration.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("session")]
        public SessionConfiguration SessionConfiguration { get; private set; }

        [JsonIgnore]
        [Obsolete("use SessionConfiguration")]
        public SessionConfiguration Options => SessionConfiguration;
    }
}
