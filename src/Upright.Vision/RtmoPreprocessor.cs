using Microsoft.ML.OnnxRuntime.Tensors;

namespace Upright.Vision;

public sealed record RtmoInput(DenseTensor<float> Tensor, double ResizeRatio);

public static class RtmoPreprocessor
{
    public const int InputWidth = 640;
    public const int InputHeight = 640;
    public const byte PaddingValue = 114;

    public static RtmoInput CreateInput(VisionFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        frame.Validate();

        double ratio = Math.Min(
            (double)InputWidth / frame.Width,
            (double)InputHeight / frame.Height);
        int resizedWidth = Math.Max(1, (int)(frame.Width * ratio));
        int resizedHeight = Math.Max(1, (int)(frame.Height * ratio));

        var tensor = new DenseTensor<float>(
            new[] { 1, 3, InputHeight, InputWidth });
        Fill(tensor.Buffer.Span, PaddingValue);

        for (int targetY = 0; targetY < resizedHeight; targetY++)
        {
            double sourceY = ((targetY + 0.5) / ratio) - 0.5;
            int y0 = Math.Clamp((int)Math.Floor(sourceY), 0, frame.Height - 1);
            int y1 = Math.Min(y0 + 1, frame.Height - 1);
            double yWeight = Math.Clamp(sourceY - y0, 0, 1);

            for (int targetX = 0; targetX < resizedWidth; targetX++)
            {
                double sourceX = ((targetX + 0.5) / ratio) - 0.5;
                int x0 = Math.Clamp((int)Math.Floor(sourceX), 0, frame.Width - 1);
                int x1 = Math.Min(x0 + 1, frame.Width - 1);
                double xWeight = Math.Clamp(sourceX - x0, 0, 1);

                for (int channel = 0; channel < 3; channel++)
                {
                    float top = Lerp(
                        ReadChannel(frame, x0, y0, channel),
                        ReadChannel(frame, x1, y0, channel),
                        xWeight);
                    float bottom = Lerp(
                        ReadChannel(frame, x0, y1, channel),
                        ReadChannel(frame, x1, y1, channel),
                        xWeight);
                    tensor[0, channel, targetY, targetX] =
                        Lerp(top, bottom, yWeight);
                }
            }
        }

        return new RtmoInput(tensor, ratio);
    }

    private static byte ReadChannel(
        VisionFrame frame,
        int x,
        int y,
        int channel)
    {
        int index = ((y * frame.Width) + x) * 4;
        return frame.BgraPixels[index + channel];
    }

    private static float Lerp(float start, float end, double weight) =>
        (float)(start + ((end - start) * weight));

    private static void Fill(Span<float> values, float value)
    {
        foreach (ref float item in values)
        {
            item = value;
        }
    }
}
