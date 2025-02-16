// Licensed under the MIT License. See LICENSE in the project root for license information.

using OpenAI.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.Realtime
{
    public sealed class RealtimeSession : IDisposable
    {
        /// <summary>
        /// Enable or disable logging.
        /// </summary>
        public bool EnableDebug { get; set; }

        /// <summary>
        /// The timeout in seconds to wait for a response from the server.
        /// </summary>
        public int EventTimeout { get; set; } = 30;

        [Obsolete("Use Configuration")]
        public SessionConfiguration Options => Configuration;

        /// <summary>
        /// The configuration options for the session.
        /// </summary>
        public SessionConfiguration Configuration { get; internal set; }

        #region Internal

        internal event Action<IServerEvent> OnEventReceived;

        internal event Action<Exception> OnError;

        private readonly WebSocket websocketClient;
        private readonly ConcurrentQueue<IRealtimeEvent> events = new();
        private readonly object eventLock = new();

        private bool isCollectingEvents;

        internal RealtimeSession(WebSocket wsClient, bool enableDebug)
        {
            websocketClient = wsClient;
            websocketClient.OnMessage += OnMessage;
            EnableDebug = enableDebug;
        }

        private void OnMessage(DataFrame dataFrame)
        {
            if (dataFrame.Type == OpCode.Text)
            {
                if (EnableDebug)
                {
                    Console.WriteLine(dataFrame.Text);
                }

                try
                {
                    var @event = JsonSerializer.Deserialize<IServerEvent>(dataFrame.Text, OpenAIClient.JsonSerializationOptions);

                    lock (eventLock)
                    {
                        events.Enqueue(@event);
                    }

                    OnEventReceived?.Invoke(@event);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    OnError?.Invoke(e);
                }
            }
        }

        ~RealtimeSession() => Dispose(false);

        #region IDisposable

        private bool isDisposed;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!isDisposed && disposing)
            {
                websocketClient.OnMessage -= OnMessage;
                websocketClient.Dispose();
                isDisposed = true;
            }
        }

        #endregion IDisposable

        internal async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            var connectTcs = new TaskCompletionSource<State>();
            websocketClient.OnOpen += OnWebsocketClientOnOpen;
            websocketClient.OnError += OnWebsocketClientOnError;

            try
            {
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                // don't call async because it is blocking until connection is closed.
                websocketClient.Connect();
                await connectTcs.Task.WithCancellation(cancellationToken).ConfigureAwait(false);

                if (websocketClient.State != State.Open)
                {
                    throw new Exception($"Failed to start new session! {websocketClient.State}");
                }
            }
            finally
            {
                websocketClient.OnOpen -= OnWebsocketClientOnOpen;
                websocketClient.OnError -= OnWebsocketClientOnError;
            }

            return;

            void OnWebsocketClientOnError(Exception e)
                => connectTcs.TrySetException(e);
            void OnWebsocketClientOnOpen()
                => connectTcs.TrySetResult(websocketClient.State);
        }

        #endregion Internal

        /// <summary>
        /// Receive callback updates from the server
        /// </summary>
        /// <typeparam name="T"><see cref="IRealtimeEvent"/> to subscribe for updates to.</typeparam>
        /// <param name="sessionEvent">The event to receive updates for.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Task"/>.</returns>
        /// <exception cref="Exception">If <see cref="ReceiveUpdatesAsync{T}"/> is already running.</exception>
        public async Task ReceiveUpdatesAsync<T>(Action<T> sessionEvent, CancellationToken cancellationToken) where T : IRealtimeEvent
        {
            try
            {
                lock (eventLock)
                {
                    if (isCollectingEvents)
                    {
                        throw new Exception($"{nameof(ReceiveUpdatesAsync)} is already running!");
                    }

                    isCollectingEvents = true;
                }

                do
                {
                    try
                    {
                        T @event = default;

                        lock (eventLock)
                        {
                            if (events.TryDequeue(out var dequeuedEvent) &&
                                dequeuedEvent is T typedEvent)
                            {
                                @event = typedEvent;
                            }
                        }

                        if (@event != null)
                        {
                            sessionEvent(@event);
                        }

                        await Task.Yield();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                } while (!cancellationToken.IsCancellationRequested && websocketClient.State == State.Open);
            }
            finally
            {
                lock (eventLock)
                {
                    isCollectingEvents = false;
                }
            }
        }

        /// <summary>
        /// Receive callback updates from the server
        /// </summary>
        /// <typeparam name="T"><see cref="IRealtimeEvent"/> to subscribe for updates to.</typeparam>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="IAsyncEnumerable{T}"/>.</returns>
        /// <exception cref="Exception">If <see cref="ReceiveUpdatesAsync{T}"/> is already running.</exception>
        public async IAsyncEnumerable<T> ReceiveUpdatesAsync<T>([EnumeratorCancellation] CancellationToken cancellationToken) where T : IRealtimeEvent
        {
            try
            {
                lock (eventLock)
                {
                    if (isCollectingEvents)
                    {
                        throw new Exception($"{nameof(ReceiveUpdatesAsync)} is already running!");
                    }

                    isCollectingEvents = true;
                }

                do
                {
                    T @event = default;

                    lock (eventLock)
                    {
                        if (events.TryDequeue(out var dequeuedEvent) &&
                            dequeuedEvent is T typedEvent)
                        {
                            @event = typedEvent;
                        }
                    }

                    if (@event != null)
                    {
                        yield return @event;
                    }

                    await Task.Yield();
                } while (!cancellationToken.IsCancellationRequested && websocketClient.State == State.Open);
            }
            finally
            {
                lock (eventLock)
                {
                    isCollectingEvents = false;
                }
            }
        }

        /// <summary>
        /// Send a client event to the server.
        /// </summary>
        /// <typeparam name="T"><see cref="IClientEvent"/> to send to the server.</typeparam>
        /// <param name="event">The event to send.</param>
        public async void Send<T>(T @event) where T : IClientEvent
            => await SendAsync(@event).ConfigureAwait(false);

        /// <summary>
        /// Send a client event to the server.
        /// </summary>
        /// <typeparam name="T"><see cref="IClientEvent"/> to send to the server.</typeparam>
        /// <param name="event">The event to send.</param>
        /// <param name="sessionEvents">Optional, <see cref="Action{IServerEvent}"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Task{IServerEvent}"/>.</returns>
        public async Task<IServerEvent> SendAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IClientEvent
            => await SendAsync(@event, null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a client event to the server.
        /// </summary>
        /// <typeparam name="T"><see cref="IClientEvent"/> to send to the server.</typeparam>
        /// <param name="event">The event to send.</param>
        /// <param name="sessionEvents">Optional, <see cref="Action{IServerEvent}"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Task{IServerEvent}"/>.</returns>
        public async Task<IServerEvent> SendAsync<T>(T @event, Action<IServerEvent> sessionEvents, CancellationToken cancellationToken = default) where T : IClientEvent
        {
            if (websocketClient.State != State.Open)
            {
                throw new Exception($"Websocket connection is not open! {websocketClient.State}");
            }

            IClientEvent clientEvent = @event;
            var payload = clientEvent.ToJsonString();

            if (EnableDebug)
            {
                if (@event is not InputAudioBufferAppendRequest)
                {
                    Console.WriteLine(payload);
                }
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(EventTimeout));
            using var eventCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var tcs = new TaskCompletionSource<IServerEvent>();
            eventCts.Token.Register(() => tcs.TrySetCanceled());
            OnEventReceived += EventCallback;

            lock (eventLock)
            {
                events.Enqueue(clientEvent);
            }

            var eventId = Guid.NewGuid().ToString("N");

            if (EnableDebug)
            {
                if (@event is not InputAudioBufferAppendRequest)
                {
                    Console.WriteLine($"[{eventId}] sending {clientEvent.Type}");
                }
            }

            await websocketClient.SendAsync(payload, cancellationToken).ConfigureAwait(false);

            if (EnableDebug)
            {
                if (@event is not InputAudioBufferAppendRequest)
                {
                    Console.WriteLine($"[{eventId}] sent {clientEvent.Type}");
                }
            }

            if (@event is InputAudioBufferAppendRequest)
            {
                // no response for this client event
                return default;
            }

            var response = await tcs.Task.WithCancellation(eventCts.Token).ConfigureAwait(false);

            if (EnableDebug)
            {
                Console.WriteLine($"[{eventId}] received {response.Type}");
            }

            return response;

            void EventCallback(IServerEvent serverEvent)
            {
                sessionEvents?.Invoke(serverEvent);

                try
                {
                    if (serverEvent is RealtimeEventError serverError)
                    {
                        tcs.TrySetException(serverError);
                        OnEventReceived -= EventCallback;
                        return;
                    }

                    switch (clientEvent)
                    {
                        case UpdateSessionRequest when serverEvent is SessionResponse sessionResponse:
                            Configuration = sessionResponse.SessionConfiguration;
                            Complete();
                            return;
                        case InputAudioBufferCommitRequest when serverEvent is InputAudioBufferCommittedResponse:
                        case InputAudioBufferClearRequest when serverEvent is InputAudioBufferClearedResponse:
                        case ConversationItemCreateRequest when serverEvent is ConversationItemCreatedResponse:
                        case ConversationItemTruncateRequest when serverEvent is ConversationItemTruncatedResponse:
                        case ConversationItemDeleteRequest when serverEvent is ConversationItemDeletedResponse:
                            Complete();
                            return;
                        case CreateResponseRequest when serverEvent is RealtimeResponse serverResponse:
                        {
                            if (serverResponse.Response.Status == RealtimeResponseStatus.InProgress)
                            {
                                return;
                            }

                            if (serverResponse.Response.Status != RealtimeResponseStatus.Completed)
                            {
                                tcs.TrySetException(new Exception(serverResponse.Response.StatusDetails.Error?.ToString() ?? serverResponse.Response.StatusDetails.Reason));
                            }
                            else
                            {
                                Complete();
                            }

                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                return;

                void Complete()
                {
                    if (EnableDebug)
                    {
                        Console.WriteLine($"{clientEvent.Type} -> {serverEvent.Type}");
                    }

                    tcs.TrySetResult(serverEvent);
                    OnEventReceived -= EventCallback;
                }
            }
        }
    }
}
