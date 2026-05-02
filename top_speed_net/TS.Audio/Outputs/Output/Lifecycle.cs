using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace TS.Audio
{
    public sealed partial class AudioOutput
    {
        private const int NativeDisposalGraceFrames = 2;

        private readonly ConcurrentQueue<AudioLifecycleWork> _lifecycleQueue;
        private readonly List<AudioLifecycleWork> _deferredLifecycle;

        internal void EnqueueLifecycle(Action action, string? name = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _lifecycleQueue.Enqueue(new AudioLifecycleWork(action, name, 0));
        }

        internal void EnqueueDeferredLifecycle(Action action, string? name = null, int graceFrames = NativeDisposalGraceFrames)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            _lifecycleQueue.Enqueue(new AudioLifecycleWork(action, name, Math.Max(0, graceFrames)));
        }

        private void DrainLifecycle(bool force = false)
        {
            while (_lifecycleQueue.TryDequeue(out var work))
            {
                if (!force && work.DelayFrames > 0)
                {
                    _deferredLifecycle.Add(work);
                    continue;
                }

                ExecuteLifecycleWork(work);
            }

            if (_deferredLifecycle.Count == 0)
                return;

            for (var i = _deferredLifecycle.Count - 1; i >= 0; i--)
            {
                var work = _deferredLifecycle[i];
                if (!force && work.DelayFrames > 0)
                {
                    work.DelayFrames--;
                    continue;
                }

                _deferredLifecycle.RemoveAt(i);
                ExecuteLifecycleWork(work);
            }
        }

        internal void AttachSourceToGraph(AudioBus bus, SourcePlayer player, SteamAudioSpatialModifier? spatializer)
        {
            if (spatializer != null)
                player.AddModifier(spatializer);
            bus.Mixer.AddComponent(player);
        }

        internal void AttachBusToGraph(AudioBus? parent, SoundFlow.Components.Mixer mixer)
        {
            if (parent == null)
                _playbackDevice.MasterMixer.AddComponent(mixer);
            else
                parent.Mixer.AddComponent(mixer);
        }

        internal void DetachBusFromGraph(AudioBus? parent, SoundFlow.Components.Mixer mixer)
        {
            if (parent == null)
                _playbackDevice.MasterMixer.RemoveComponent(mixer);
            else
                parent.Mixer.RemoveComponent(mixer);
        }

        internal void DetachSourceFromGraph(AudioBus bus, SourcePlayer player, SteamAudioSpatialModifier? spatializer)
        {
            if (spatializer != null)
                player.RemoveModifier(spatializer);
            bus.Mixer.RemoveComponent(player);
        }

        internal void ReplaceSourceSpatializer(SourcePlayer player, SteamAudioSpatialModifier? oldSpatializer, SteamAudioSpatialModifier? newSpatializer)
        {
            if (oldSpatializer != null)
                player.RemoveModifier(oldSpatializer);
            if (newSpatializer != null)
                player.AddModifier(newSpatializer);
        }

        private void ExecuteLifecycleWork(AudioLifecycleWork work)
        {
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
                    "Audio lifecycle operation failed.",
                    new Dictionary<string, object?>
                    {
                        ["operation"] = work.Name,
                        ["exceptionType"] = ex.GetType().FullName,
                        ["message"] = ex.Message
                    });
            }
        }

        private sealed class AudioLifecycleWork
        {
            public AudioLifecycleWork(Action action, string? name, int delayFrames)
            {
                Action = action;
                Name = name;
                DelayFrames = delayFrames;
            }

            public Action Action { get; }
            public string? Name { get; }
            public int DelayFrames { get; set; }
        }
    }
}
