// Licensed under the MIT License. See LICENSE in the project root for license information.

using OpenAI.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.Realtime
{
    public sealed class RealtimeEndpoint : OpenAIBaseEndpoint
    {
        internal RealtimeEndpoint(OpenAIClient client) : base(client) { }

        protected override string Root => "realtime";

        protected override bool? IsWebSocketEndpoint => true;

        /// <summary>
        /// Creates a new realtime session with the provided <see cref="SessionConfiguration"/> options.
        /// </summary>
        /// <param name="configuration"><see cref="SessionConfiguration"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="RealtimeSession"/>.</returns>
        public async Task<RealtimeSession> CreateSessionAsync(SessionConfiguration configuration = null, CancellationToken cancellationToken = default)
        {
            string model = string.IsNullOrWhiteSpace(configuration?.Model) ? Models.Model.GPT4oRealtime : configuration!.Model;
            var queryParameters = new Dictionary<string, string>();

            if (client.OpenAIClientSettings.IsAzureOpenAI)
            {
                queryParameters["deployment"] = model;
            }
            else
            {
                queryParameters["model"] = model;
            }

            var session = new RealtimeSession(client.CreateWebSocket(GetUrl(queryParameters: queryParameters)), EnableDebug);
            var sessionCreatedTcs = new TaskCompletionSource<SessionResponse>();

            try
            {
                session.OnEventReceived += OnEventReceived;
                session.OnError += OnError;
                await session.ConnectAsync(cancellationToken).ConfigureAwait(false);
                var sessionResponse = await sessionCreatedTcs.Task.WithCancellation(cancellationToken).ConfigureAwait(false);
                session.Configuration = sessionResponse.SessionConfiguration;
                await session.SendAsync(new UpdateSessionRequest(configuration), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                session.OnError -= OnError;
                session.OnEventReceived -= OnEventReceived;
            }

            return session;

            void OnError(Exception e)
            {
                sessionCreatedTcs.SetException(e);
            }

            void OnEventReceived(IRealtimeEvent @event)
            {
                try
                {
                    switch (@event)
                    {
                        case RealtimeConversationResponse:
                            Console.WriteLine("[conversation.created]");
                            break;
                        case SessionResponse sessionResponse:
                            if (sessionResponse.Type == "session.created")
                            {
                                sessionCreatedTcs.TrySetResult(sessionResponse);
                            }
                            break;
                        case RealtimeEventError realtimeEventError:
                            sessionCreatedTcs.TrySetException(new Exception(realtimeEventError.Error.Message));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    sessionCreatedTcs.TrySetException(e);
                }
            }
        }
    }
}
