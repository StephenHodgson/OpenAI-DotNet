﻿// Licensed under the MIT License. See LICENSE in the project root for license information.

using OpenAI.Extensions;
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OpenAI.Realtime
{
    public sealed class RealtimeSession : IDisposable
    {
        public bool EnableDebug { get; set; }

        public int EventTimeout { get; set; } = 30;

        public Options Options { get; internal set; }

        #region Internal

        internal event Action<IServerEvent> OnEventReceived;

        internal event Action<Exception> OnError;

        private readonly WebSocket websocketClient;
        private readonly ConcurrentQueue<IRealtimeEvent> events = new();
        private readonly object eventLock = new();

        private bool collectEvents;

        internal RealtimeSession(WebSocket wsClient, bool enableDebug)
        {
            websocketClient = wsClient;
            EnableDebug = enableDebug;
            websocketClient.OnMessage += OnMessage;
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
                        if (collectEvents)
                        {
                            events.Enqueue(@event);
                        }
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
            websocketClient.OnOpen += OnWebsocketClientOnOnOpen;
            websocketClient.OnError += OnWebsocketClientOnOnError;

            try
            {
                // ReSharper disable once MethodHasAsyncOverloadWithCancellation
                // don't call async because it is blocking until connection is closed.
                websocketClient.Connect();
                await connectTcs.Task; // TODO .WithCancellation(cancellationToken).ConfigureAwait(true);

                if (websocketClient.State != State.Open)
                {
                    throw new Exception($"Failed to start new session! {websocketClient.State}");
                }
            }
            finally
            {
                websocketClient.OnOpen -= OnWebsocketClientOnOnOpen;
                websocketClient.OnError -= OnWebsocketClientOnOnError;
            }

            return;

            void OnWebsocketClientOnOnError(Exception e)
                => connectTcs.TrySetException(e);
            void OnWebsocketClientOnOnOpen()
                => connectTcs.TrySetResult(websocketClient.State);
        }

        #endregion Internal

        public async Task ReceiveUpdatesAsync<T>(Action<T> sessionEvent, CancellationToken cancellationToken) where T : IRealtimeEvent
        {
            try
            {
                lock (eventLock)
                {
                    if (collectEvents)
                    {
                        Console.WriteLine($"{nameof(ReceiveUpdatesAsync)} is already running!");
                        return;
                    }

                    collectEvents = true;
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
                    collectEvents = false;
                }
            }
        }

        public async void Send<T>(T @event) where T : IClientEvent
            => await SendAsync(@event);

        public async Task<IServerEvent> SendAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IClientEvent
            => await SendAsync(@event, null, cancellationToken);

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
                if (collectEvents)
                {
                    events.Enqueue(clientEvent);
                }
            }

            var eventId = Guid.NewGuid().ToString("N");

            if (EnableDebug)
            {
                if (@event is not InputAudioBufferAppendRequest)
                {
                    Console.WriteLine($"[{eventId}] sending {clientEvent.Type}");
                }
            }

            await websocketClient.SendAsync(payload, cancellationToken);

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

            var response = await tcs.Task; // TODO .WithCancellation(eventCts.Token);

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
                            Options = sessionResponse.Options;
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
                                tcs.TrySetException(new Exception(serverResponse.Response.StatusDetails.Error.ToString()));
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
