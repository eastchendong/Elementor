using System.Collections.Generic;
using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    [ExecuteInEditMode]
    [AddComponentMenu(" Script Boy/Procedural Book/Render Texture Factory")]
    public class RenderTextureFactory : MonoBehaviour
    {
        [SerializeField] RenderTexture m_Sample;

        static int m_ID;

        internal RenderTexture Create()
        {
            RenderTexture texture = new RenderTexture(m_Sample);
            texture.name = "Temp Texture " + m_ID++;
            texture.hideFlags = HideFlags.DontSave;
            return texture;
        }

        internal bool CompareWithSample(RenderTexture texture)
        {
            return TextureUtility.CompareProperties(texture, m_Sample);
        }
    }
}