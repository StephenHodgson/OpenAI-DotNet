﻿// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OpenAI.Realtime
{
    public sealed class RealtimeResponseResource
    {
        /// <summary>
        /// The unique ID of the response.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("id")]
        public string Id { get; private set; }

        /// <summary>
        /// The object type, must be "realtime.response".
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("object")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string Object { get; private set; }

        /// <summary>
        /// The status of the response ("in_progress").
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("status")]
        [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
        [JsonConverter(typeof(Extensions.JsonStringEnumConverter<RealtimeResponseStatus>))]
        public RealtimeResponseStatus Status { get; private set; }

        /// <summary>
        /// Additional details about the status.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("status_details")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public StatusDetails StatusDetails { get; private set; }

        /// <summary>
        /// The list of output items generated by the response.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("output")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public IReadOnlyList<ConversationItem> Output { get; private set; }

        [JsonInclude]
        [JsonPropertyName("metadata")]
        public IReadOnlyDictionary<string, object> Metadata { get; private set; }

        /// <summary>
        /// Usage statistics for the Response, this will correspond to billing.
        /// A Realtime API session will maintain a conversation context and append new Items to the Conversation,
        /// thus output from previous turns (text and audio tokens) will become the input for later turns.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("usage")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public Usage Usage { get; private set; }

        /// <summary>
        /// Which conversation the response is added to, determined by the `conversation`
        /// field in the `response.create` event. If `auto`, the response will be added to
        /// the default conversation and the value of `conversation_id` will be an id like
        /// `conv_1234`. If `none`, the response will not be added to any conversation and
        /// the value of `conversation_id` will be `null`. If responses are being triggered
        /// by server VAD, the response will be added to the default conversation, thus
        /// the `conversation_id` will be an id like `conv_1234`.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("conversation_id")]
        public string ConversationId { get; private set; }

        /// <summary>
        /// The voice the model used to respond.
        /// Current voice options are `alloy`, `ash`, `ballad`, `coral`, `echo` `sage`, `shimmer` and `verse`.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("voice")]
        public string Voice { get; private set; }

        /// <summary>
        /// The set of modalities the model used to respond. If there are multiple modalities,
        /// the model will pick one, for example if `modalities` is `["text", "audio"]`, the model
        /// could be responding in either text or audio.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("modalities")]
        [JsonConverter(typeof(ModalityConverter))]
        public Modality Modalities { get; private set; }

        /// <summary>
        /// The format of output audio. Options are `pcm16`, `g711_ulaw`, or `g711_alaw`.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("output_audio_format")]
        [JsonConverter(typeof(Extensions.JsonStringEnumConverter<RealtimeAudioFormat>))]
        public RealtimeAudioFormat OutputAudioFormat { get; private set; }

        /// <summary>
        /// Sampling temperature for the model, limited to [0.6, 1.2]. Defaults to 0.8.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("temperature")]
        public float Temperature { get; private set; }

        /// <summary>
        ///  Maximum number of output tokens for a single assistant response, inclusive of tool calls, that was used in this response.
        /// </summary>
        [JsonInclude]
        [JsonPropertyName("max_output_tokens")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public object MaxOutputTokens { get; private set; }
    }
}
