using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    [AddComponentMenu(" Script Boy/Procedural Book/Sprite Page Content")]
    class SpritePageContent : PageContent
    {
        [Tooltip("Set a sprite as the page image.")]
        [SerializeField] Sprite m_Sprite;

        internal override Texture texture
        {
            get
            {
                if (m_Sprite) return m_Sprite.texture;

                return null;
            }
        }

        protected override Vector4 textureST
        {
            get
            {
                if (m_Sprite) return TextureUtility.GetST(m_Sprite);

                return base.textureST;
            }
        }
    }
}