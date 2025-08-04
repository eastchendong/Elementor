using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ScriptBoy.ProceduralBook
{
    [ExecuteInEditMode]
    [AddComponentMenu(" Script Boy/Procedural Book/Live Page Content")]
    public sealed class LivePageContent : PageContent
    {
        [Tooltip("Set a Render Texture as the page image.")]
        [SerializeField] RenderTexture m_PageRT;
        [Tooltip("Set a Render Texture Factory instead of setting the Page RT.")]
        [SerializeField] RenderTextureFactory m_PageRTFactory;
        [Tooltip("Set the Camera that renders to the Page RT.")]
        [SerializeField] Camera m_Camera;
        [Tooltip("Set the canvas that is rendered to the Page RT.")]
        [SerializeField] Canvas m_Canvas;
        [Tooltip("Set the Video Player that renders to the Video RT.")]
        [SerializeField] VideoPlayer m_VideoPlayer;
        [Tooltip("Set a Render Texture for the Video Player.")]
        [SerializeField] RenderTexture m_VideoRT;
        [Tooltip("Set a Render Texture Factory instead of setting the Video RT.")]
        [SerializeField] RenderTextureFactory m_VideoRTFactory;
        [Tooltip("Set a Raw Image to display the Video RT on it.")]
        [SerializeField] RawImage m_VideoRTTargetUI;


        RenderTexture m_TempPageRT;
        RenderTexture m_TempVideoRT;
        List<LivePageCanvasRaycaster> m_CanvasRaycasters = new List<LivePageCanvasRaycaster>();

        bool m_WasVideoPlaying;

        internal override Texture texture => m_PageRT;

        void OnEnable()
        {
            RefreshRTs();
        }

        void OnValidate()
        {
            RefreshRTs();
        }

        void RefreshRTs()
        {
            if (m_PageRTFactory)
            {
                if (m_PageRT != m_TempPageRT) m_PageRT = null;
                if (m_PageRT && !m_PageRTFactory.CompareWithSample(m_PageRT)) DestroyPageRT();
                if (!m_PageRT) m_PageRT = m_TempPageRT = m_PageRTFactory.Create();
            }

            if (m_VideoRTFactory)
            {
                if (m_VideoRT != m_TempVideoRT) m_VideoRT = null;
                if (m_VideoRT && !m_VideoRTFactory.CompareWithSample(m_VideoRT)) DestroyVideoRT();
                if (!m_VideoRT) m_VideoRT = m_TempVideoRT = m_VideoRTFactory.Create();
            }

            if (m_Camera)
            {
                m_Camera.targetTexture = m_PageRT;
                if (m_VideoPlayer) m_VideoPlayer.targetTexture = m_VideoRT;
            }
            else if (m_VideoPlayer) m_VideoPlayer.targetTexture = m_PageRT;

            if (m_VideoRTTargetUI) m_VideoRTTargetUI.texture = m_VideoRT;
        }

        void OnDestroy()
        {
            if (m_PageRT == m_TempPageRT) DestroyPageRT();
            if (m_VideoRT == m_TempVideoRT) DestroyVideoRT();
        }

        void DestroyPageRT()
        {
            if (m_Camera && m_Camera.targetTexture == m_PageRT) m_Camera.targetTexture = null;
            if (m_VideoPlayer && m_VideoPlayer.targetTexture == m_PageRT) m_VideoPlayer.targetTexture = null;

            ObjectUtility.Destroy(m_PageRT);
            m_PageRT = m_TempPageRT = null;
        }

        void DestroyVideoRT()
        {
            if (m_VideoPlayer && m_VideoPlayer.targetTexture == m_VideoRT) m_VideoPlayer.targetTexture = null;
            if (m_VideoRTTargetUI) m_VideoRTTargetUI.texture = null;

            ObjectUtility.Destroy(m_VideoRT);
            m_VideoRT = m_TempVideoRT = null;
        }

        internal Vector2 TextureCoordToScreenCoord(Vector2 coord)
        {
            coord.x *= m_PageRT.width;
            coord.y *= m_PageRT.height;
            return coord;
        }

        internal void RegisterCanvasRaycaster(LivePageCanvasRaycaster livePageCanvasRaycaster)
        {
            if (m_CanvasRaycasters.Contains(livePageCanvasRaycaster)) return;

            m_CanvasRaycasters.Add(livePageCanvasRaycaster);
        }

        protected override bool IsPointOverUI(Vector2 textureCoord)
        {
            m_CanvasRaycasters.RemoveAll((e) => e == null);

            foreach (var canvasRaycaster in m_CanvasRaycasters)
            {
                if(canvasRaycaster.IsPointOverUI(TextureCoordToScreenCoord(textureCoord))) return true;
            }

            return false;
        }

        protected override void OnInit()
        {
            RefreshRTs();

            if (!Application.isPlaying) return;

            if (m_Camera)
            {
                m_Camera.Render();
                m_Camera.enabled = false;
            }

            if (m_VideoPlayer)
            {
                m_WasVideoPlaying = m_VideoPlayer.playOnAwake || m_VideoPlayer.isPlaying;
                m_VideoPlayer.Play();
                m_VideoPlayer.Pause();
            }

            if (m_Canvas && m_CanvasRaycasters.Count == 0)
            {
                GameObject gameObject = m_Canvas.gameObject;
                var raycaster = gameObject.GetComponent<GraphicRaycaster>();
                if (raycaster) ObjectUtility.Destroy(raycaster);

                var canvasRaycaster = gameObject.AddComponent<LivePageCanvasRaycaster>();
                canvasRaycaster.hideFlags = HideFlags.DontSave;
                canvasRaycaster.Init(this);
                m_CanvasRaycasters.Clear();
                m_CanvasRaycasters.Add(canvasRaycaster);
            }
        }

        protected override void OnActiveChanged()
        {
            if (!Application.isPlaying) return;

            if (m_Camera)
            {
                m_Camera.enabled = isActive;
            }

            if (m_VideoPlayer)
            {
                if (isActive)
                {
                    if (m_WasVideoPlaying) m_VideoPlayer.Play();
                }
                else
                {
                    m_WasVideoPlaying = m_VideoPlayer.isPlaying;
                    m_VideoPlayer.Pause();
                }
            }
        }
    }


    public class LivePageCanvasRaycaster : GraphicRaycaster
    {
        LivePageContent m_LivePage;

        /// <summary>
        /// The currently active drawing camera that is viewing the book. It is required to handle mouse input for UI elements. If it is null, Camera.main is used instead. If Camera.main is also null, it throws an error. You should either set it or ensure that the tag of your camera is 'MainCamera' in the Inspector window.
        /// </summary>
        public new static Camera camera { get; set; }

        static List<RaycastResult> s_RaycastResultList = new List<RaycastResult>();

        internal void Init(LivePageContent livePage)
        {
            m_LivePage = livePage;
        }

        new void Start()
        {
            base.Start();

            if (m_LivePage != null) return;

            Transform parent = transform.parent;
            while (parent != null)
            {
                var livePage = parent.GetComponent<LivePageContent>();
                if (livePage != null)
                {
                    livePage.RegisterCanvasRaycaster(this);
                    m_LivePage = livePage;

                    return;
                }
                parent = parent.parent;
            }
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            Vector2 m;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                m = new Vector2(Screen.width, Screen.height) / 2;
            }
            else
            {
                m = eventData.position;
            }

            Camera camera = LivePageCanvasRaycaster.camera;

            if (camera == null)
            {
                camera = Camera.main;
            }

            if (camera == null)
            {
                Debug.LogError("Camera.main is null! LivePageCanvasRaycaster.camera is null! Please set the tag of your camera to 'MainCamera' in the Inspector window, or create a script and set the static property 'LivePageCanvasRaycaster.camera' to your camera.");
                return;
            }

            Ray ray = camera.ScreenPointToRay(m);

            if (m_LivePage == null) return;
            BookContent bookContent = m_LivePage.bookContent;
            if (m_LivePage == null) return;
            Book book = bookContent.book;
            if (book == null) return;


            if (eventData.dragging)
            {
                if (eventData.pointerDrag.transform.IsChildOf(transform))
                {
                    Vector3 textureCoordinate = book.GetTextureCoordinate(ray, m_LivePage);
                    eventData.position = m_LivePage.TextureCoordToScreenCoord(textureCoordinate);
                    base.Raycast(eventData, resultAppendList);
                }
            }
            else if (book.Raycast(ray, out BookRaycastHit hit))
            {
                if (hit.pageContent == (m_LivePage as IPageContent))
                {
                    Vector2 prevPosition = eventData.position;
                    eventData.position = m_LivePage.TextureCoordToScreenCoord(hit.textureCoordinate);
                    base.Raycast(eventData, resultAppendList);
                    if (!eventData.dragging && !HasDragHandler(resultAppendList))
                    {
                        eventData.position = prevPosition;
                    }
                }
            }
        }

        bool HasDragHandler(List<RaycastResult> resultAppendList)
        {
            int n = resultAppendList.Count;
            for (int i = 0; i < n; i++)
            {
                GameObject g = resultAppendList[i].gameObject;
                for (int j = 0; j < 2; j++)
                {
                    if (g.GetComponent<IDragHandler>() != null)
                    {
                        return true;
                    }
                    g = g.transform.parent.gameObject;
                }
            }

            return false;
        }


        public bool IsPointOverUI(Vector2 screenCoord)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);

            eventData.position = screenCoord;
            s_RaycastResultList.Clear();
            base.Raycast(eventData, s_RaycastResultList);
            int n = s_RaycastResultList.Count;
            for (int i = 0; i < n; i++)
            {
                GameObject g = s_RaycastResultList[i].gameObject;

                for (int j = 0; j < 2; j++)
                {
                    if (g.GetComponent<IPointerClickHandler>() != null) return true;
                    if (g.GetComponent<IDragHandler>() != null) return true;
                    g = g.transform.parent.gameObject;
                }
            }

            return false;
        }
    }
}