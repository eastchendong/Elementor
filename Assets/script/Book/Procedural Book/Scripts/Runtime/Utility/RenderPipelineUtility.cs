using UnityEngine.Rendering;

namespace ScriptBoy.ProceduralBook
{
    static class RenderPipelineUtility
    {
        public static RenderPipelineNames name { get; } = GetName(GraphicsSettings.defaultRenderPipeline);

        static RenderPipelineNames GetName(RenderPipelineAsset asset)
        {
            if (asset == null) return RenderPipelineNames.BuiltIn;
            string name = asset.GetType().Name;
            if (name == "UniversalRenderPipelineAsset") return RenderPipelineNames.URP;
            if (name == "LightweightPipelineAsset") return RenderPipelineNames.LWRP;
            if (name == "HDRenderPipelineAsset") return RenderPipelineNames.HDRP;
            return RenderPipelineNames.CustomRP;
        }
    }

    enum RenderPipelineNames
    {
        BuiltIn, URP, LWRP, HDRP, CustomRP
    }
}