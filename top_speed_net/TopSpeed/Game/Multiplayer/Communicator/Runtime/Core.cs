using System;
using System.Collections.Generic;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio;
using TopSpeed.Audio;
using TopSpeed.Core.Multiplayer;
using TopSpeed.Input;
using TopSpeed.Protocol;
using TS.Audio;

namespace TopSpeed.Game.Multiplayer.Communicator
{
    internal sealed partial class MultiplayerCommunicatorRuntime : IDisposable
    {
        private const int MaxCapturedSamples = ProtocolConstants.VoiceSampleRate * 2;
        private const int MaxQueuedRemoteFrames = 16;

        private readonly AudioManager _audio;
        private readonly DriveSettings _settings;
        private readonly IMultiplayerRuntime _multiplayer;
        private readonly IInputService _input;
        private readonly Func<Network.MultiplayerSession?> _getSession;
        private readonly object _captureLock = new object();
        private readonly List<short> _capturedSamples = new List<short>(ProtocolConstants.VoiceSampleRate);
        private readonly Dictionary<uint, RemoteVoiceStream> _remoteStreams = new Dictionary<uint, RemoteVoiceStream>();
        private readonly Random _random = new Random();
        private readonly VoiceEncoder _encoder = new VoiceEncoder();
        private readonly short[] _captureFrame = new short[ProtocolConstants.VoiceSampleRate * ProtocolConstants.VoiceFrameMs / 1000];

        private MiniAudioEngine? _captureEngine;
        private AudioCaptureDevice? _captureDevice;
        private string _captureDeviceName = string.Empty;
        private int _captureChannels = 1;
        private long _captureSampleCount;
        private bool _captureFirstFrameLogged;
        private long _txFrameCount;
        private long _txByteCount;

        private Network.MultiplayerSession? _boundSession;
        private bool _transmitting;
        private bool _micCueOpen;
        private uint _streamId;
        private uint _nextStreamId = 1;
        private ushort _activeFrequencyTenths;
        private bool _activePushToTalk;
        private uint _timestampMs;

        private SoundAsset? _micOpenSound;
        private SoundAsset? _micCloseSound;
        private readonly SoundAsset?[] _pttCues = new SoundAsset?[12];

        public MultiplayerCommunicatorRuntime(
            AudioManager audio,
            DriveSettings settings,
            IMultiplayerRuntime multiplayer,
            IInputService input,
            Func<Network.MultiplayerSession?> getSession)
        {
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _multiplayer = multiplayer ?? throw new ArgumentNullException(nameof(multiplayer));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _getSession = getSession ?? throw new ArgumentNullException(nameof(getSession));
        }

        public void BindSession(Network.MultiplayerSession? session)
        {
            if (ReferenceEquals(_boundSession, session))
                return;

            Disarm();
            _boundSession = session;
            ClearRemoteStreams();
        }

        public void Dispose()
        {
            Disarm();
            _boundSession = null;
            ClearRemoteStreams();
            DisposeCachedSounds();
        }
    }
}
