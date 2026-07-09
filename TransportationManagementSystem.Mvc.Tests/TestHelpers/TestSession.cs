using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TransportationManagementSystem.Mvc.Tests.TestHelpers
{
    /// <summary>
    /// Minimal in-memory ISession fake for unit tests that need a real
    /// session object (e.g. GridBuilder/TripGridBuilder, which read/write
    /// route state via session.GetObject/SetObject extension methods).
    /// Backed by a plain Dictionary instead of a mock, since ISession's
    /// TryGetValue(string, out byte[]) shape is awkward to set up with Moq.
    /// </summary>
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public bool IsAvailable => true;
        public string Id => "test-session-id";
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _store.TryGetValue(key, out value);
    }
}

