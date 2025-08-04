using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class TextureUtility
    {
        static Texture s_WhiteTexture;

        static Texture whiteTexture
        {
            get
            {
                if (s_WhiteTexture == null)
                {
                    s_WhiteTexture = Texture2D.whiteTexture;
                }
                return s_WhiteTexture;
            }
        }

        public static Texture FixNull(Texture texture)
        {
            if (texture == null) return whiteTexture;

            return texture;
        }

        public static Vector4 GetST(Sprite sprite)
        {
            float uMin = 1;
            float uMax = 0;
            float vMin = 1;
            float vMax = 0;

            foreach (var uv in sprite.uv)
            {
                uMin = Mathf.Min(uMin, uv.x);
                uMax = Mathf.Max(uMax, uv.x);
                vMin = Mathf.Min(vMin, uv.y);
                vMax = Mathf.Max(vMax, uv.y);
            }

            return new Vector4(uMax - uMin, vMax - vMin, uMin, vMin);
        }

        public static Vector4 XFlipST(Vector4 st)
        {
            st.x = -st.x;
            st.z += -st.x;
            return st;
        }

        public static Vector4 YFlipST(Vector4 st)
        {
            st.y = -st.y;
            st.w += -st.y;
            return st;
        }

        public static bool CompareProperties(RenderTexture a, RenderTexture b)
        {
            return
                a.width == b.width &&
                a.height == b.height &&
                a.format == b.format &&
                a.useMipMap == b.useMipMap &&
                a.antiAliasing == b.antiAliasing &&
                a.filterMode == b.filterMode &&
                a.wrapMode == b.wrapMode;
        }
    }
}