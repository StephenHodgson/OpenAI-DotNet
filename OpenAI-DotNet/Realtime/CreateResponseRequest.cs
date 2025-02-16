// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text.Json.Serialization;

namespace OpenAI.Realtime
{
    /// <summary>
    /// This event instructs the server to create a Response, which means triggering model inference.
    /// When in Server VAD mode, the server will create Responses automatically.
    /// A Response will include at least one Item, and may have two, in which case the second will be a function call.
    /// These Items will be appended to the conversation history. The server will respond with a response.created event,
    /// events for Items and content created, and finally a response.done event to indicate the Response is complete.
    /// The response.create event includes inference configuration like instructions, and temperature.
    /// These fields will override the Session's configuration for this Response only.
    /// </summary>
    public sealed class CreateResponseRequest : BaseRealtimeEvent, IClientEvent
    {
        public CreateResponseRequest() { }

        [Obsolete("Use the constructor that takes RealtimeResponseCreateParams.")]
        public CreateResponseRequest(Options options)
        {
            Options = options;
        }

        /// <summary>
        /// </summary>
        /// <param name="options"></param>
        public CreateResponseRequest(RealtimeResponseCreateParams options)
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
        public override string Type { get; protected set; } = "response.create";

        [JsonInclude]
        [JsonPropertyName("response")]
        public RealtimeResponseCreateParams Options { get; private set; }
    }
}
