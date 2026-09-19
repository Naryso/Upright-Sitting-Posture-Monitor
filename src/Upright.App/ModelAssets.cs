using System.IO;

namespace Upright.App;

public static class ModelAssets
{
    public const string DetectorFileName =
        "yolox-nano-person-416x416.onnx";

    public const string PoseFileName =
        "rtmpose-t-body17-256x192.onnx";

    public static (string DetectorPath, string PosePath) Resolve()
    {
        string modelDirectory = Path.Combine(AppContext.BaseDirectory, "models");
        return (
            Path.Combine(modelDirectory, DetectorFileName),
            Path.Combine(modelDirectory, PoseFileName));
    }
}
