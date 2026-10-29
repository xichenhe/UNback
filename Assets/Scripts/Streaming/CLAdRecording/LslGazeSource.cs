using System;
using System.Collections.Generic;
using System.Diagnostics;
using LSL;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CLAd.Recording
{
    /// <summary>
    /// The newest combined-gaze sample of a VarjoEyeTrackingLSL-layout stream, and whether
    /// it can be used: channel 9 = combined gaze valid (0/1), channels 10–12 = combined
    /// forward in HMD space (the main camera's local frame, Unity axes).
    /// </summary>
    public sealed class GazeSampleTracker
    {
        public const int ValidChannel = 9;
        public const int ForwardChannel = 10;
        public const int MinChannelCount = 13;

        private readonly double _maxAgeSeconds;
        private bool _hasSample;
        private bool _valid;
        private Vector3 _forward;
        private double _timestamp;

        public GazeSampleTracker(double maxAgeSeconds = 0.1)
        {
            _maxAgeSeconds = maxAgeSeconds;
        }

        /// <summary>Keeps this sample as the newest; samples shorter than 13 channels are ignored.</summary>
        public void Offer(double[] sample, double timestamp)
        {
            if (sample == null || sample.Length < MinChannelCount)
                return;

            _hasSample = true;
            _valid = sample[ValidChannel] != 0;
            _forward = new Vector3((float)sample[ForwardChannel], (float)sample[ForwardChannel + 1],
                (float)sample[ForwardChannel + 2]);
            _timestamp = timestamp;
        }

        public void Clear() => _hasSample = false;

        /// <summary>
        /// The gaze direction, unless there is no sample, it is flagged invalid, it is older
        /// than the maximum age at <paramref name="lslNow"/>, or its direction is zero.
        /// </summary>
        public bool TryGetForward(double lslNow, out Vector3 hmdForward)
        {
            bool usable = _hasSample && _valid && lslNow - _timestamp <= _maxAgeSeconds
                          && _forward.sqrMagnitude > 1e-12f;
            hmdForward = usable ? _forward : default;
            return usable;
        }
    }

    /// <summary>
    /// Reads the app's own gaze stream through an LSL inlet, so no Varjo gaze API is called
    /// and Varjo's sample queue stays with the 200 Hz VarjoEyeTrackingLSL streamer.
    ///
    /// Poll() never blocks and never throws. It attaches when a stream of that name with at
    /// least 13 channels appears, drains it into the tracker, and when the outlet goes away
    /// (an empty source_id makes the inlet unrecoverable) drops the inlet and waits for the
    /// stream to be published again. The lost outlet is never re-opened, because the
    /// resolver keeps listing a vanished stream for a few seconds.
    ///
    /// Only streams published from this machine are read: the app's own gaze stream is
    /// always local, and one from another machine on the network would be someone else's eyes.
    /// </summary>
    public sealed class LslGazeSource : IDisposable
    {
        private const double ForgetAfterSeconds = 5.0;
        private const int InletBufferSeconds = 1;

        private readonly string _streamName;
        private readonly GazeSampleTracker _tracker;
        private readonly string _localHostName;
        private readonly double _retrySeconds;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly HashSet<string> _lostUids = new HashSet<string>();
        private readonly HashSet<string> _ignoredHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ContinuousResolver _resolver;
        private StreamInlet _inlet;
        private string _uid;
        private double[] _buffer;
        private double _nextAttemptSeconds;
        private bool _attachFailureLogged;

        /// <param name="localHostName">This machine's name as LSL reports it, e.g. the
        /// hostname() of an outlet this app created.</param>
        public LslGazeSource(string streamName, GazeSampleTracker tracker, string localHostName,
            double retrySeconds = 1.0)
        {
            _streamName = streamName;
            _tracker = tracker;
            _localHostName = localHostName;
            _retrySeconds = retrySeconds;
        }

        public bool IsAttached => _inlet != null;

        public void Poll()
        {
            try
            {
                if (_inlet == null && !TryAttach())
                    return;

                double timestamp;
                while ((timestamp = _inlet.pull_sample(_buffer, 0.0)) != 0.0)
                    _tracker.Offer(_buffer, timestamp);
            }
            catch (Exception e)
            {
                Drop(e);
            }
        }

        public void Dispose()
        {
            DisposeInlet();
            _resolver?.Dispose();
            _resolver = null;
        }

        private bool TryAttach()
        {
            double now = _clock.Elapsed.TotalSeconds;
            if (now < _nextAttemptSeconds)
                return false;
            _nextAttemptSeconds = now + _retrySeconds;

            if (_resolver == null)
                _resolver = new ContinuousResolver("name", _streamName, ForgetAfterSeconds);

            StreamInfo[] found = _resolver.results();
            try
            {
                foreach (StreamInfo info in found)
                {
                    if (info.channel_count() < GazeSampleTracker.MinChannelCount || _lostUids.Contains(info.uid()))
                        continue;

                    string host = info.hostname();
                    if (!string.Equals(host, _localHostName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (_ignoredHosts.Add(host))
                            Debug.LogWarning($"CLAd scene luminance: ignoring gaze stream '{_streamName}' from " +
                                             $"another machine ({host}); only this machine's stream is read.");
                        continue;
                    }

                    _inlet = new StreamInlet(info, InletBufferSeconds);
                    _uid = info.uid();
                    _buffer = new double[info.channel_count()];
                    _attachFailureLogged = false;
                    Debug.Log($"CLAd scene luminance: reading gaze from '{_streamName}'.");
                    return true;
                }
                return false;
            }
            finally
            {
                foreach (StreamInfo info in found)
                    info.Dispose();
            }
        }

        private void Drop(Exception error)
        {
            bool wasAttached = _inlet != null;
            if (_uid != null)
                _lostUids.Add(_uid);
            _uid = null;
            DisposeInlet();
            _tracker.Clear();

            if (wasAttached)
            {
                Debug.LogWarning($"CLAd scene luminance: lost gaze stream '{_streamName}' ({error.Message}); " +
                                 "gaze reads as invalid until it is published again.");
            }
            else if (!_attachFailureLogged)
            {
                _attachFailureLogged = true;
                Debug.LogWarning($"CLAd scene luminance: cannot read gaze stream '{_streamName}' ({error.Message}); retrying.");
            }
        }

        private void DisposeInlet()
        {
            _inlet?.Dispose();
            _inlet = null;
        }
    }
}
