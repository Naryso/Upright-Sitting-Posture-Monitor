namespace Upright.Vision.Tests;

public sealed class RtmoPreprocessorTests
{
    [Fact]
    public void CameraSizedFrameIsCopiedAndBottomPadded()
    {
        byte[] pixels = new byte[640 * 480 * 4];
        pixels[0] = 10;
        pixels[1] = 20;
        pixels[2] = 30;
        pixels[3] = 255;
        var frame = new VisionFrame(pixels, 640, 480);

        RtmoInput input = RtmoPreprocessor.CreateInput(frame);

        Assert.Equal(1, input.ResizeRatio);
        Assert.Equal(10, input.Tensor[0, 0, 0, 0]);
        Assert.Equal(20, input.Tensor[0, 1, 0, 0]);
        Assert.Equal(30, input.Tensor[0, 2, 0, 0]);
        Assert.Equal(114, input.Tensor[0, 0, 480, 0]);
        Assert.Equal(114, input.Tensor[0, 2, 639, 639]);
    }

    [Fact]
    public void InvalidBufferIsRejected()
    {
        var frame = new VisionFrame(new byte[3], 1, 1);

        Assert.Throws<ArgumentException>(() => RtmoPreprocessor.CreateInput(frame));
    }
}
