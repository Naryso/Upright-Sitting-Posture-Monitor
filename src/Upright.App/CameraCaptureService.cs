using System.Diagnostics;
using Upright.Core;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace Upright.App;

public sealed record CameraDevice(string Id, string Name);

public sealed record CameraFrame(
    byte[] BgraPixels,
    int Width,
    int Height,
    TimeSpan CaptureAge);

public sealed class CameraCaptureService : IAsyncDisposable
{
    private readonly SemaphoreSlim _frameGate = new(1, 1);
    private MediaCapture? _capture;
    private MediaFrameReader? _reader;
    private Stopwatch? _clock;
    private long _lastDeliveredTimestamp;

    public event EventHandler<CameraFrame>? FrameArrived;
    public event EventHandler<Exception>? CaptureFailed;

    public TimeSpan MinimumDeliveryInterval { get; set; } =
        PostureEngine.SlouchingFrameInterval;

    public static async Task<IReadOnlyList<CameraDevice>> GetCamerasAsync()
    {
        DeviceInformationCollection devices =
            await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

        return devices
            .Select(device => new CameraDevice(device.Id, device.Name))
            .ToArray();
    }

    public async Task StartAsync(string cameraId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cameraId);
        await StopAsync();

        var capture = new MediaCapture();
        var settings = new MediaCaptureInitializationSettings
        {
            VideoDeviceId = cameraId,
            StreamingCaptureMode = StreamingCaptureMode.Video,
            MemoryPreference = MediaCaptureMemoryPreference.Cpu,
        };

        try
        {
            await capture.InitializeAsync(settings);

            MediaFrameSource source = capture.FrameSources.Values
                .Where(candidate =>
                    candidate.Info.MediaStreamType == MediaStreamType.VideoPreview ||
                    candidate.Info.MediaStreamType == MediaStreamType.VideoRecord)
                .Where(candidate =>
                    candidate.Info.SourceKind == MediaFrameSourceKind.Color)
                .OrderBy(candidate =>
                    candidate.Info.MediaStreamType == MediaStreamType.VideoPreview ? 0 : 1)
                .First();

            MediaFrameFormat? format = source.SupportedFormats
                .Where(candidate => candidate.VideoFormat is not null)
                .OrderBy(candidate => FormatDistance(candidate, 640, 480))
                .FirstOrDefault();

            if (format is not null)
            {
                await source.SetFormatAsync(format);
            }

            MediaFrameReader reader = await capture.CreateFrameReaderAsync(
                source,
                MediaEncodingSubtypes.Bgra8);
            reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            reader.FrameArrived += OnFrameArrived;

            MediaFrameReaderStartStatus status = await reader.StartAsync();
            if (status != MediaFrameReaderStartStatus.Success)
            {
                reader.FrameArrived -= OnFrameArrived;
                reader.Dispose();
                capture.Dispose();
                throw new InvalidOperationException(
                    $"Camera frame reader failed to start: {status}.");
            }

            _capture = capture;
            _reader = reader;
            _clock = Stopwatch.StartNew();
            Interlocked.Exchange(ref _lastDeliveredTimestamp, 0);
        }
        catch
        {
            capture.Dispose();
            throw;
        }
    }

    public async Task StopAsync()
    {
        MediaFrameReader? reader = Interlocked.Exchange(ref _reader, null);
        MediaCapture? capture = Interlocked.Exchange(ref _capture, null);
        _clock = null;
        Interlocked.Exchange(ref _lastDeliveredTimestamp, 0);

        if (reader is not null)
        {
            reader.FrameArrived -= OnFrameArrived;
            await reader.StopAsync();
            reader.Dispose();
        }

        capture?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _frameGate.Dispose();
        GC.SuppressFinalize(this);
    }

    private async void OnFrameArrived(
        MediaFrameReader sender,
        MediaFrameArrivedEventArgs args)
    {
        long now = Stopwatch.GetTimestamp();
        long previous = Volatile.Read(ref _lastDeliveredTimestamp);
        long minimumTicks = (long)(
            MinimumDeliveryInterval.TotalSeconds * Stopwatch.Frequency);
        if (previous != 0 && now - previous < minimumTicks)
        {
            return;
        }

        if (Interlocked.CompareExchange(
                ref _lastDeliveredTimestamp,
                now,
                previous) != previous)
        {
            return;
        }

        if (!await _frameGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            using MediaFrameReference? frame = sender.TryAcquireLatestFrame();
            SoftwareBitmap? original = frame?.VideoMediaFrame?.SoftwareBitmap;
            if (original is null)
            {
                return;
            }

            using SoftwareBitmap converted = SoftwareBitmap.Convert(
                original,
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore);
            int byteCount = checked(converted.PixelWidth * converted.PixelHeight * 4);
            var buffer = new Windows.Storage.Streams.Buffer((uint)byteCount)
            {
                Length = (uint)byteCount,
            };
            converted.CopyToBuffer(buffer);

            byte[] pixels = new byte[byteCount];
            using DataReader reader = DataReader.FromBuffer(buffer);
            reader.ReadBytes(pixels);

            FrameArrived?.Invoke(
                this,
                new CameraFrame(
                    pixels,
                    converted.PixelWidth,
                    converted.PixelHeight,
                    _clock?.Elapsed ?? TimeSpan.Zero));
        }
        catch (ObjectDisposedException)
        {
            // Expected when the reader is stopped while a callback is active.
        }
        catch (Exception exception)
        {
            try
            {
                CaptureFailed?.Invoke(this, exception);
            }
            catch
            {
                // Never allow a diagnostic subscriber to terminate the capture callback.
            }
        }
        finally
        {
            _frameGate.Release();
        }
    }

    private static long FormatDistance(MediaFrameFormat format, int width, int height)
    {
        if (format.VideoFormat is null)
        {
            return long.MaxValue;
        }

        long widthDelta = Math.Abs((long)format.VideoFormat.Width - width);
        long heightDelta = Math.Abs((long)format.VideoFormat.Height - height);
        return widthDelta + heightDelta;
    }
}
