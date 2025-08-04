using UnityEngine;
#if UNITY_EDITOR
using UnityEngine.Rendering;
using UnityEditor;
#endif
namespace ScriptBoy.ProceduralBook
{
    //[CreateAssetMenu(menuName = " Script Boy/Procedural Book/ Book Resources", fileName = "Book Resources")]
    public sealed class BookResources : ScriptableObject
    {
        [HideInInspector]
        [SerializeField] Material m_DefaultPaperMaterial;

        [HideInInspector]
        [SerializeField] Material m_DefaultMetalMaterial;

        [HideInInspector]
        [SerializeField] Material m_DefaultPaperInstancingMaterial;


        static BookResources s_Instance;
        static BookResources instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<BookResources>("Book Resources");
                }

                return s_Instance;
            }
        }


        internal static Material FixNullPaperMaterial(Material material)
        {
            if (material != null) return material;
            if (instance != null) return instance.m_DefaultPaperMaterial;
            return MaterialUtility.defaultMaterial;
        }

        internal static Material FixNullMetalMaterial(Material material)
        {
            if (material != null) return material;
            if (instance != null) return instance.m_DefaultMetalMaterial;
            return MaterialUtility.defaultMaterial;
        }

        internal static Material FixNullPaperInstancingMaterial(Material material)
        {
            if (material != null) return material;
            if (instance != null) return instance.m_DefaultPaperInstancingMaterial;
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Create Materials")]
        void Create()
        {
            if (m_DefaultPaperMaterial) AssetDatabase.RemoveObjectFromAsset(m_DefaultPaperMaterial);
            if (m_DefaultMetalMaterial) AssetDatabase.RemoveObjectFromAsset(m_DefaultMetalMaterial);

            m_DefaultPaperMaterial = new Material(MaterialUtility.defaultMaterial);
            m_DefaultPaperMaterial.name = "Paper";

            m_DefaultMetalMaterial = new Material(MaterialUtility.defaultMaterial);
            m_DefaultMetalMaterial.name = "Metal";

            m_DefaultPaperInstancingMaterial = new Material(MaterialUtility.defaultMaterial);
            m_DefaultPaperInstancingMaterial.name = "PaperInstancing";
            m_DefaultPaperInstancingMaterial.shader = FindDefaultPaperInstancingShader();

            AssetDatabase.AddObjectToAsset(m_DefaultPaperMaterial, this);
            AssetDatabase.AddObjectToAsset(m_DefaultMetalMaterial, this);
            AssetDatabase.AddObjectToAsset(m_DefaultPaperInstancingMaterial, this);

            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        static Shader FindDefaultPaperInstancingShader()
        {
            switch (RenderPipelineUtility.name)
            {
                case RenderPipelineNames.BuiltIn: return Shader.Find("Hidden/ScriptBoy/ProceduralBook/PaperInstancingBuiltIn");
                case RenderPipelineNames.URP: return Shader.Find("Hidden/ScriptBoy/ProceduralBook/PaperInstancingURP");
                case RenderPipelineNames.LWRP: return null;
                case RenderPipelineNames.HDRP: return Shader.Find("Hidden/ScriptBoy/ProceduralBook/PaperInstancingHDRP");
                case RenderPipelineNames.CustomRP:
                default: return null;
            }
        }

        [ContextMenu("Hide Materials")]
        void HideMaterials()
        {
            m_DefaultPaperMaterial.hideFlags = HideFlags.HideInHierarchy;
            m_DefaultMetalMaterial.hideFlags = HideFlags.HideInHierarchy;
            m_DefaultPaperInstancingMaterial.hideFlags = HideFlags.HideInHierarchy;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        [ContextMenu("Show Materials")]
        void ShowMaterials()
        {
            m_DefaultPaperMaterial.hideFlags = HideFlags.None;
            m_DefaultMetalMaterial.hideFlags = HideFlags.None;
            m_DefaultPaperInstancingMaterial.hideFlags = HideFlags.None;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.delayCall += UpgradeDefaultMaterials;
            RenderPipelineManager.activeRenderPipelineTypeChanged += UpgradeDefaultMaterials;
        }

        static void UpgradeDefaultMaterials()
        {
            instance.UpgradeMaterials();
        }

        void UpgradeMaterials()
        {
            bool changed = false;

            changed |= FixMaterialShader(m_DefaultPaperMaterial);
            changed |= FixMaterialShader(m_DefaultMetalMaterial);
            changed |= FixPaperInstancingMaterial(m_DefaultPaperInstancingMaterial);

            if (changed)
            {
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssetIfDirty(this);
            }
        }

        bool FixMaterialShader(Material material)
        {
            Shader shader = MaterialUtility.defaultMaterial.shader;

            if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
                return true;
            }
            return false;
        }

        bool FixPaperInstancingMaterial(Material material)
        {
            Shader shader = FindDefaultPaperInstancingShader();

            if (material.shader != shader)
            {
                material.shader = shader;
                EditorUtility.SetDirty(material);
                return true;
            }
            return false;
        }
#endif
    }
}

