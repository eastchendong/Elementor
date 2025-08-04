using UnityEngine;
using UnityEngine.Rendering;

namespace ScriptBoy.ProceduralBook
{
    static class MaterialUtility
    {
        static Material s_DefaultMaterial;

        static Shader s_ErrorShader;

        public static Material defaultMaterial
        {
            get
            {
                var renderPipeline = GraphicsSettings.defaultRenderPipeline;

                if (renderPipeline == null)
                {
                    if (s_DefaultMaterial == null)
                    {
                        s_DefaultMaterial = new Material(Shader.Find("Standard"));
                        s_DefaultMaterial.hideFlags = HideFlags.DontSave;
                    }
                    return s_DefaultMaterial;
                }

                return renderPipeline.defaultMaterial;
            }
        }

        public static Shader errorShader
        {
            get
            {
                if (s_ErrorShader == null)
                {
                    s_ErrorShader = Shader.Find("Hidden/InternalErrorShader");
                }

                return s_ErrorShader;
            }
        }

        public static string defaultMainTextureName { get; } = GetDefaultMainTextureName(RenderPipelineUtility.name);

        public static string defaultMainColorName { get; } = GetDefaultMainColorName(RenderPipelineUtility.name);

        static string GetDefaultMainTextureName(RenderPipelineNames name)
        {
            switch (name)
            {
                case RenderPipelineNames.BuiltIn:
                    return "_MainTex";
                case RenderPipelineNames.URP:
                case RenderPipelineNames.LWRP:
                    return "_BaseMap";
                case RenderPipelineNames.HDRP:
                    return "_BaseColorMap";
                case RenderPipelineNames.CustomRP:
                default:
                    return "_BaseMap";
            }
        }

        static string GetDefaultMainColorName(RenderPipelineNames name)
        {
            switch (name)
            {
                case RenderPipelineNames.BuiltIn:
                    return "_Color";
                case RenderPipelineNames.URP:
                case RenderPipelineNames.HDRP:
                case RenderPipelineNames.LWRP:
                case RenderPipelineNames.CustomRP:
                default:
                    return "_BaseColor";
            }
        }

        public static Material FixNull(Material material)
        {
            if (material == null) return defaultMaterial;

            return material;
        }

        public static Material[] CreateArray(Material material, int arrayLength)
        {
            Material[] array = new Material[arrayLength];
            for (int i = 0; i < arrayLength; i++)
            {
                array[i] = material;
            }
            return array;
        }

        public static int GetMainTextureID(Material material)
        {
            int i = FindPropertyIndexByFlag(material.shader, ShaderPropertyFlags.MainTexture);
            if (i != -1) return material.shader.GetPropertyNameId(i);
            return Shader.PropertyToID(defaultMainTextureName);
        }

        public static int GetMainTextureSTID(Material material)
        {
            return Shader.PropertyToID(GetMainTextureName(material) + "_ST");
        }

        static string GetMainTextureName(Material material)
        {
            int i = FindPropertyIndexByFlag(material.shader, ShaderPropertyFlags.MainTexture);
            if (i != -1) return material.shader.GetPropertyName(i);
            return defaultMainTextureName;
        }

        public static int GetMainColorID(Material material)
        {
            int i = FindPropertyIndexByFlag(material.shader, ShaderPropertyFlags.MainColor);
            if (i != -1) return material.shader.GetPropertyNameId(i);
            return Shader.PropertyToID(defaultMainColorName);
        }

        static int FindPropertyIndexByFlag(Shader shader, ShaderPropertyFlags flag)
        {
            int count = shader.GetPropertyCount();
            for (int i = 0; i < count; i++)
            {
                if (shader.GetPropertyFlags(i).HasFlag(flag))
                {
                    return i;
                }
            }
            return -1;
        }
    }
}