using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScriptBoy.ProceduralBook
{
    interface IPageContent
    {
        Texture texture { get; }
        Vector4 textureST { get; }

        bool IsPointOverUI(Vector2 textureCoord);

        void Init(BookContent bookContent);
        void SetActive(bool active);
    }

    public abstract class PageContent : MonoBehaviour, IPageContent
    {
        BookContent m_BookContent;
        public BookContent bookContent => m_BookContent;

        /// <summary>
        /// Indicates whether the page is active, meaning it is currently visible or is about to be visible.
        /// </summary>
        public bool isActive { get; private set; }
        internal abstract Texture texture { get; }
        protected virtual Vector4 textureST => new Vector4(1, 1, 0, 0);
        internal virtual bool isShareable => true;

        Texture IPageContent.texture => texture;
        Vector4 IPageContent.textureST => textureST;

        /// <summary>
        /// Callback invoked when the active state of the page changes.
        /// </summary>
        public System.Action onActiveChangedCallback { get; set; }

        protected virtual void OnInit() { }
        protected virtual void OnActiveChanged() { }
        protected virtual bool IsPointOverUI(Vector2 textureCoord) => false;


        void IPageContent.Init(BookContent bookContent)
        {
            if (!isShareable && m_BookContent != null && m_BookContent != bookContent)
            {
                Debug.LogError("The page content is already in use. It can only be assigned to one book content.", this);

                m_BookContent.book?.Clear();
            }

            m_BookContent = bookContent;
            isActive = false;
            OnInit();
        }

        void IPageContent.SetActive(bool active)
        {
            if (isActive != active)
            {
                isActive = active;
                OnActiveChanged();
                onActiveChangedCallback?.Invoke();
            }
        }

        bool IPageContent.IsPointOverUI(Vector2 textureCoord) => IsPointOverUI(textureCoord);
    }

    class SpritePageContent2 : IPageContent
    {
        public SpritePageContent2(Sprite sprite)
        {
            m_Sprite = sprite;
        }

        Sprite m_Sprite;

        Texture IPageContent.texture
        {
            get
            {
                if (m_Sprite) return m_Sprite.texture;

                return null;
            }
        }

        Vector4 IPageContent.textureST
        {
            get
            {
                if (m_Sprite) return TextureUtility.GetST(m_Sprite);

                return new Vector4(1, 1, 0, 0);
            }
        }

        bool IPageContent.IsPointOverUI(Vector2 textureCoord) => false;

        void IPageContent.Init(BookContent book) { }

        void IPageContent.SetActive(bool active) { }
    }
}