using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    static class TextureUtility
    {
        public static Texture CreateColor(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.hideFlags = HideFlags.DontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}

