using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        private const int MaxQueuedControlOperations = 8192;

        private readonly ConcurrentQueue<AudioControlWork> _controlQueue;
        private int _queuedControlOperations;

        internal bool EnqueueControl(Action action, string? name = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var queued = Interlocked.Increment(ref _queuedControlOperations);
            if (queued > MaxQueuedControlOperations)
            {
                Interlocked.Decrement(ref _queuedControlOperations);
                _diagnostics.Emit(
                    AudioDiagnosticLevel.Error,
                    AudioDiagnosticKind.OutputBackendAnomaly,
                    AudioDiagnosticEntityType.Output,
                    Name,
                    null,
                    null,
                    "Audio control queue overflow.",
                    new Dictionary<string, object?>
                    {
                        ["operation"] = name,
                        ["queued"] = queued
                    });
                return false;
            }

            _controlQueue.Enqueue(new AudioControlWork(action, name));
            return true;
        }

        private void DrainControl()
        {
            while (_controlQueue.TryDequeue(out var work))
            {
                Interlocked.Decrement(ref _queuedControlOperations);
                try
                {
                    work.Action();
                }
                catch (Exception ex)
                {
                    _diagnostics.Emit(
                        AudioDiagnosticLevel.Error,
                        AudioDiagnosticKind.OutputBackendAnomaly,
                        AudioDiagnosticEntityType.Output,
                        Name,
                        null,
                        null,
                        "Audio control operation failed.",
                        new Dictionary<string, object?>
                        {
                            ["operation"] = work.Name,
                            ["exceptionType"] = ex.GetType().FullName,
                            ["message"] = ex.Message
                        });
                }
            }
        }

        private sealed class AudioControlWork
        {
            public AudioControlWork(Action action, string? name)
            {
                Action = action;
                Name = name;
            }

            public Action Action { get; }
            public string? Name { get; }
        }
    }
}
