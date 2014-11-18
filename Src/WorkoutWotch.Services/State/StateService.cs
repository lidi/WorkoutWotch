﻿using System;
using WorkoutWotch.Services.Contracts.State;
using System.Threading.Tasks;
using Akavache;
using WorkoutWotch.Services.Contracts.Logger;
using Kent.Boogaart.HelperTrinity.Extensions;
using System.Reactive.Threading.Tasks;
using System.Collections.Generic;
using WorkoutWotch.Utility;
using System.Diagnostics;
using System.Linq;

namespace WorkoutWotch.Services.State
{
    public sealed class StateService : IStateService
    {
        private readonly IBlobCache blobCache;
        private readonly ILogger logger;
        private readonly IList<Func<IStateService, Task>> saveCallbacks;
        private readonly object sync;

        public StateService(IBlobCache blobCache, ILoggerService loggerService)
        {
            blobCache.AssertNotNull("blobCache");
            loggerService.AssertNotNull("loggerService");

            this.blobCache = blobCache;
            this.logger = loggerService.GetLogger(this.GetType());
            this.saveCallbacks = new List<Func<IStateService, Task>>();
            this.sync = new object();
        }

        public Task<T> GetAsync<T>(string key)
        {
            key.AssertNotNull("key");
            return this.blobCache.GetObjectAsync<T>(key).ToTask();
        }

        public Task SetAsync<T>(string key, T value)
        {
            key.AssertNotNull("key");
            return this.blobCache.InsertObject<T>(key, value).ToTask();
        }

        public Task RemoveAsync<T>(string key)
        {
            key.AssertNotNull("key");
            return this.blobCache.InvalidateObject<T>(key).ToTask();
        }

        public async Task SaveAsync()
        {
            IList<Task> saveTasks;

            lock (this.sync)
            {
                saveTasks = this.saveCallbacks.Select(x => x(this)).ToList();
            }

            try
            {
                await Task.WhenAll(saveTasks);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save.");
            }
        }

        public IDisposable RegisterSaveCallback(Func<IStateService, Task> saveTaskFactory)
        {
            saveTaskFactory.AssertNotNull("saveTaskFactory");

            lock (this.sync)
            {
                this.saveCallbacks.Add(saveTaskFactory);
            }

            return new RegistrationHandle(this, saveTaskFactory);
        }

        private void UnregisterSaveCallback(Func<IStateService, Task> saveTaskFactory)
        {
            Debug.Assert(saveTaskFactory != null);

            lock (this.sync)
            {
                this.saveCallbacks.Remove(saveTaskFactory);
            }
        }

        private sealed class RegistrationHandle : DisposableBase
        {
            private readonly StateService owner;
            private readonly Func<IStateService, Task> saveTaskFactory;

            public RegistrationHandle(StateService owner, Func<IStateService, Task> saveTaskFactory)
            {
                Debug.Assert(owner != null);
                Debug.Assert(saveTaskFactory != null);

                this.owner = owner;
                this.saveTaskFactory = saveTaskFactory;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    this.owner.UnregisterSaveCallback(this.saveTaskFactory);
                }
            }
        }
    }
}
