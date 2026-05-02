using System;
using System.Collections.Generic;
using SoundFlow.Abstracts;
using SoundFlow.Modifiers;

namespace TS.Audio
{
    public sealed partial class AudioBus
    {
        public void SetEffectsEnabled(bool enabled)
        {
            lock (_effectLock)
            {
                _effectsEnabled = enabled;
                QueueEffectStateApplyUnsafe();
            }

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.BusEffectsEnabledChanged,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                enabled ? "Audio bus effects enabled." : "Audio bus effects bypassed.",
                new Dictionary<string, object?> { ["effectsEnabled"] = enabled },
                () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
        }

        public BusEffect AddEffect(AudioEffectProcessCallback process, string? name = null)
        {
            return InsertEffect(_effects.Count, process, name);
        }

        public BusEffect InsertEffect(int index, AudioEffectProcessCallback process, string? name = null)
        {
            ThrowIfDisposed();
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            return InsertEffectInternal(index, new CallbackEffectModifier(process), name);
        }

        public BusEffect AddLowPassEffect(float cutoffFrequency, string? name = null)
        {
            return InsertLowPassEffect(_effects.Count, cutoffFrequency, name);
        }

        public BusEffect InsertLowPassEffect(int index, float cutoffFrequency, string? name = null)
        {
            ThrowIfDisposed();
            return InsertEffectInternal(index, new LowPassModifier(_output.SoundFlowFormat, cutoffFrequency), name ?? "low-pass");
        }

        public BusEffect AddHighPassEffect(float cutoffFrequency, string? name = null)
        {
            return InsertHighPassEffect(_effects.Count, cutoffFrequency, name);
        }

        public BusEffect InsertHighPassEffect(int index, float cutoffFrequency, string? name = null)
        {
            ThrowIfDisposed();
            return InsertEffectInternal(index, new HighPassModifier(_output.SoundFlowFormat, cutoffFrequency), name ?? "high-pass");
        }

        public BusEffect AddDelayEffect(int delaySamples = 48000, float feedback = 0.5f, float wetMix = 0.3f, float cutoff = 5000f, string? name = null)
        {
            return InsertDelayEffect(_effects.Count, delaySamples, feedback, wetMix, cutoff, name);
        }

        public BusEffect InsertDelayEffect(int index, int delaySamples = 48000, float feedback = 0.5f, float wetMix = 0.3f, float cutoff = 5000f, string? name = null)
        {
            ThrowIfDisposed();
            return InsertEffectInternal(index, new DelayModifier(_output.SoundFlowFormat, delaySamples, feedback, wetMix, cutoff), name ?? "delay");
        }

        public BusEffect AddReverbEffect(float wet = 0.5f, float roomSize = 0.5f, float damp = 0.5f, float width = 1f, float preDelayMs = 0f, float mix = 0.5f, string? name = null)
        {
            return InsertReverbEffect(_effects.Count, wet, roomSize, damp, width, preDelayMs, mix, name);
        }

        public BusEffect InsertReverbEffect(int index, float wet = 0.5f, float roomSize = 0.5f, float damp = 0.5f, float width = 1f, float preDelayMs = 0f, float mix = 0.5f, string? name = null)
        {
            ThrowIfDisposed();
            var modifier = new AlgorithmicReverbModifier(_output.SoundFlowFormat)
            {
                Wet = wet,
                RoomSize = roomSize,
                Damp = damp,
                Width = width,
                PreDelay = preDelayMs,
                Mix = mix
            };
            return InsertEffectInternal(index, modifier, name ?? "reverb");
        }

        public bool MoveEffect(BusEffect effect, int newIndex)
        {
            if (effect == null)
                return false;

            lock (_effectLock)
            {
                var currentIndex = _effects.IndexOf(effect);
                if (currentIndex < 0)
                    return false;

                _effects.RemoveAt(currentIndex);
                var clampedIndex = Math.Clamp(newIndex, 0, _effects.Count);
                _effects.Insert(clampedIndex, effect);
                QueueRebuildModifierChainUnsafe();
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.BusEffectMoved,
                    AudioDiagnosticEntityType.Bus,
                    _output.Name,
                    Name,
                    null,
                    "Audio bus effect moved.",
                    new Dictionary<string, object?>
                    {
                        ["from"] = currentIndex,
                        ["to"] = clampedIndex,
                        ["effect"] = effect.Name
                    },
                    () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
                return true;
            }
        }

        public bool RemoveEffectAt(int index)
        {
            lock (_effectLock)
            {
                if (index < 0 || index >= _effects.Count)
                    return false;

                var effect = _effects[index];
                _effects.RemoveAt(index);
                QueueRebuildModifierChainUnsafe();
                effect.MarkDetached();
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.BusEffectRemoved,
                    AudioDiagnosticEntityType.Bus,
                    _output.Name,
                    Name,
                    null,
                    "Audio bus effect removed.",
                    new Dictionary<string, object?>
                    {
                        ["index"] = index,
                        ["effect"] = effect.Name
                    },
                    () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
                return true;
            }
        }

        public IReadOnlyList<BusEffect> GetEffects()
        {
            lock (_effectLock)
                return new List<BusEffect>(_effects).AsReadOnly();
        }

        public void ClearEffects()
        {
            BusEffect[] removed;
            lock (_effectLock)
            {
                removed = _effects.ToArray();
                _effects.Clear();
                QueueRebuildModifierChainUnsafe();
            }

            for (var i = 0; i < removed.Length; i++)
                removed[i].MarkDetached();

            _output.Diagnostics.EmitDeferred(
                AudioDiagnosticLevel.Debug,
                AudioDiagnosticKind.BusEffectsCleared,
                AudioDiagnosticEntityType.Bus,
                _output.Name,
                Name,
                null,
                "Audio bus effects cleared.",
                new Dictionary<string, object?> { ["count"] = removed.Length },
                () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
        }

        private BusEffect InsertEffectInternal(int index, SoundModifier modifier, string? name)
        {
            lock (_effectLock)
            {
                var clampedIndex = Math.Clamp(index, 0, _effects.Count);
                var effect = new BusEffect(this, modifier, name);
                _effects.Insert(clampedIndex, effect);
                QueueRebuildModifierChainUnsafe();
                _output.Diagnostics.EmitDeferred(
                    AudioDiagnosticLevel.Debug,
                    AudioDiagnosticKind.BusEffectAdded,
                    AudioDiagnosticEntityType.Bus,
                    _output.Name,
                    Name,
                    null,
                    "Audio bus effect added.",
                    new Dictionary<string, object?>
                    {
                        ["index"] = clampedIndex,
                        ["effect"] = effect.Name
                    },
                    () => new AudioDiagnosticSnapshot(bus: CaptureSnapshot()));
                return effect;
            }
        }

        private void QueueRebuildModifierChainUnsafe()
        {
            var version = ++_effectVersion;
            _output.EnqueueControl(() => RebuildModifierChain(version), "bus-rebuild-effect-chain");
        }

        private void QueueEffectStateApplyUnsafe()
        {
            var version = ++_effectVersion;
            _output.EnqueueControl(() => ApplyEffectEnabledStates(version), "bus-apply-effect-state");
        }

        private void RebuildModifierChain(int version)
        {
            lock (_effectLock)
            {
                if (version != _effectVersion)
                    return;

                RebuildModifierChainOnOutputUnsafe();
            }
        }

        private void RebuildModifierChainOnOutputUnsafe()
        {
            for (var i = 0; i < _attachedModifiers.Count; i++)
                _mixer.RemoveModifier(_attachedModifiers[i]);

            _attachedModifiers.Clear();

            for (var i = 0; i < _effects.Count; i++)
            {
                var effect = _effects[i];
                effect.ApplyBusState(_effectsEnabled);
                _mixer.AddModifier(effect.Modifier);
                _attachedModifiers.Add(effect.Modifier);
            }
        }

        private void ApplyEffectEnabledStates(int version)
        {
            lock (_effectLock)
            {
                if (version != _effectVersion)
                    return;

                ApplyEffectEnabledStatesUnsafe();
            }
        }

        private void ApplyEffectEnabledStatesUnsafe()
        {
            for (var i = 0; i < _effects.Count; i++)
                _effects[i].ApplyBusState(_effectsEnabled);
        }
    }
}
