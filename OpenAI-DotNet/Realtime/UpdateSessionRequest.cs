// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text.Json.Serialization;

namespace OpenAI.Realtime
{
    /// <summary>
    /// Send this event to update the session’s default configuration.
    /// The client may send this event at any time to update the session configuration,
    /// and any field may be updated at any time, except for "voice".
    /// The server will respond with a session.updated event that shows the full effective configuration.
    /// Only fields that are present are updated, thus the correct way to clear a field like "instructions" is to pass an empty string.
    /// </summary>
    public sealed class UpdateSessionRequest : BaseRealtimeEvent, IClientEvent
    {
        public UpdateSessionRequest() { }

        public UpdateSessionRequest(SessionConfiguration options)
        {
            Options = options;
        }

        /// <inheritdoc />
        [JsonInclude]
        [JsonPropertyName("event_id")]
        public override string EventId { get; internal set; }

        /// <inheritdoc />
        [JsonInclude]
        [JsonPropertyName("type")]
        public override string Type { get; protected set; } = "session.update";

        /// <summary>
        /// The session configuration options.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("session")]
        public SessionConfiguration Options { get; private set; }

        [JsonIgnore]
        [Obsolete("use UpdateSessionRequest.Options")]
        public SessionConfiguration Session => Options;
    }
}
