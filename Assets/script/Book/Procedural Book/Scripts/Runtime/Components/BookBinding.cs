using UnityEngine;

namespace ScriptBoy.ProceduralBook
{
    public abstract class BookBinding : MonoBehaviour
    {
        internal abstract BookBound CreateBound(Book book, Transform root, RendererFactory rendererFactory, MeshFactory meshFactory);
    }

    abstract class BookBound
    {
        protected Book m_Book;
        protected Transform m_Root;

        internal abstract bool useSharedMeshDataForLowpoly { get; }

        internal abstract Renderer binderRenderer { get; }

        public BookBound(Book book, Transform root)
        {
            m_Book = book;
            m_Root = root;
        }

        internal abstract PaperPattern CreatePaperPattern(int quality, Vector2 size, float thickness, PaperUVMargin uvMargin, bool reduceOverdraw, bool reduceSubMeshes);

        internal abstract void ResetPaperPosition(Paper paper);
        internal abstract void UpdatePaperPosition(Paper paper);
        internal abstract void OnLateUpdate();
    }
}