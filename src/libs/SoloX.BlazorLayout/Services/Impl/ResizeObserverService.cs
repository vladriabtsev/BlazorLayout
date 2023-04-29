// ----------------------------------------------------------------------
// <copyright file="ResizeObserverService.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Reflection.Emit;
using System.Threading.Tasks;

namespace SoloX.BlazorLayout.Services.Impl
{
    /// <summary>
    /// Resize observer service implementation.
    /// </summary>
    public partial class ResizeObserverService : IResizeObserverService, IAsyncDisposable
    {
        [LoggerMessageAttribute(EventId = 0, Level = LogLevel.Debug, Message = "{Msg}")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public partial void LogDebugMessage(string msg);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        internal const string RegisterResizeCallBack = "resizeManager.registerResizeCallBack";
        internal const string UnregisterResizeCallBack = "resizeManager.unregisterResizeCallBack";
        internal const string RegisterMutationObserver = "resizeManager.registerMutationObserver";
        internal const string UnregisterMutationObserver = "resizeManager.unregisterMutationObserver";
        internal const string ProcessCallbackReferences = "resizeManager.processCallbackReferences";
        internal const string Ping = "resizeManager.ping";
        internal const string Import = "import";
        internal const string SizeObserverJsInteropFile = "./_content/SoloX.BlazorLayout/sizeObserverJsInterop.js";

        private readonly Lazy<Task<IJSObjectReference>> moduleTask;

        private readonly Dictionary<string, AsyncDisposable> disposables =
            new Dictionary<string, AsyncDisposable>();

        private readonly ILogger<ResizeObserverService> logger;

        /// <summary>
        /// Setup the service with the given jsRuntime interface.
        /// </summary>
        /// <param name="jsRuntime">The JS runtime to interact with JS sandbox.</param>
        /// <param name="logger">The logger where to log messages.</param>
        public ResizeObserverService(IJSRuntime jsRuntime, ILogger<ResizeObserverService> logger)
        {
            // Setup lazy loading of the JS size observer module.
            this.moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
               Import, SizeObserverJsInteropFile).AsTask());

            this.logger = logger;
        }

        ///<inheritdoc/>
        public async ValueTask<IAsyncDisposable> RegisterResizeCallBackAsync(
            IResizeCallBack sizeCallBack, ElementReference elementReference)
        {
            var module = await this.moduleTask.Value.ConfigureAwait(false);

            var objectRef = DotNetObjectReference.Create(new SizeCallBackProxy(sizeCallBack));

            await module.InvokeVoidAsync(RegisterResizeCallBack,
                objectRef, elementReference.Id, elementReference).ConfigureAwait(false);

            var id = $"{nameof(RegisterResizeCallBackAsync)}-{elementReference.Id}";

            var disposable = new AsyncDisposable(
                id,
                async () =>
                {
                    this.disposables.Remove(id);

                    try
                    {
                        await module.InvokeVoidAsync(UnregisterResizeCallBack,
                            TimeSpan.FromMilliseconds(500),
                            elementReference.Id).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException e)
                    {
                        this.LogDebugMessage(e.Message);
                    }

                    objectRef.Dispose();
                });

            this.disposables.Add(id, disposable);
            return disposable;
        }

        ///<inheritdoc/>
        public async ValueTask<IAsyncDisposable> RegisterMutationObserverAsync(
            ElementReference elementReference)
        {
            var module = await this.moduleTask.Value.ConfigureAwait(false);

            await module.InvokeVoidAsync(RegisterMutationObserver,
                elementReference.Id, elementReference).ConfigureAwait(false);

            var id = $"{nameof(RegisterMutationObserverAsync)}-{elementReference.Id}";

            var disposable = new AsyncDisposable(
                id,
                async () =>
                {
                    this.disposables.Remove(id);

                    try
                    {
                        await module.InvokeVoidAsync(UnregisterMutationObserver,
                            TimeSpan.FromMilliseconds(500),
                            elementReference.Id).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException e)
                    {
                        this.LogDebugMessage(e.Message);
                    }
                });

            this.disposables.Add(id, disposable);
            return disposable;
        }

        ///<inheritdoc/>
        public async ValueTask TriggerCallBackAsync()
        {
            var module = await this.moduleTask.Value.ConfigureAwait(false);

            await module.InvokeVoidAsync(ProcessCallbackReferences).ConfigureAwait(false);
        }

        ///<inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            foreach (var item in this.disposables.Values.ToArray())
            {
                await item.DisposeAsync().ConfigureAwait(false);
            }

            if (this.moduleTask.IsValueCreated)
            {
                var module = await this.moduleTask.Value.ConfigureAwait(false);

                try
                {
                    // make sure JS runtime is steel responding otherwise disposing the module may block forever.
                    await module.InvokeVoidAsync(Ping,
                        TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

                    await module.DisposeAsync().ConfigureAwait(false);
                }
                catch (TaskCanceledException e)
                {
                    this.LogDebugMessage(e.Message);
                }
            }

#pragma warning disable CA1816 // Les méthodes Dispose doivent appeler SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Les méthodes Dispose doivent appeler SuppressFinalize
        }

        internal class SizeCallBackProxy : IResizeCallBack
        {
            internal IResizeCallBack SizeCallBack { get; }

            public SizeCallBackProxy(IResizeCallBack sizeCallBack)
            {
                SizeCallBack = sizeCallBack;
            }

            [JSInvokable]
            public ValueTask ResizeAsync(int width, int height)
            {
                return SizeCallBack.ResizeAsync(width, height);
            }
        }

        private class AsyncDisposable : IAsyncDisposable
        {
            private readonly Func<ValueTask> disposeHandler;
            private readonly string id;
            private bool isDisposed;
            public AsyncDisposable(string id, Func<ValueTask> disposeHandler)
            {
                this.id = id;
                this.disposeHandler = disposeHandler;
            }

            public ValueTask DisposeAsync()
            {
#pragma warning disable CA1513 // Use ObjectDisposedException throw helper
                if (this.isDisposed)
                {
                    throw new ObjectDisposedException($"Object {this.id} already disposed.");
                }
#pragma warning restore CA1513 // Use ObjectDisposedException throw helper

                this.isDisposed = true;
                return this.disposeHandler();
            }
        }
    }
}
