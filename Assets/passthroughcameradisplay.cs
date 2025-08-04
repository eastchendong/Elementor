using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PassthroughCameraSamples;

namespace Elementor
{
    public class PassthroughCameraDisplay : MonoBehaviour
    {
        public WebCamTextureManager webcamManager;
        public Renderer quadRenderer;
        private Texture2D picture;
        
        // 添加保存图片的路径
        private string savedImagePath;
        
        // 添加对API组件的引用
        public ChemicalAssistant2 apiComponent;
        
        // Start is called before the first frame update
        void Start()
        {
            // 检查必要组件的引用
            if (webcamManager == null)
            {
                Debug.LogWarning("WebCamTextureManager未设置，请在Inspector中分配");
            }
            
            if (quadRenderer == null)
            {
                Debug.LogWarning("QuadRenderer未设置，请在Inspector中分配");
            }
        }

        // Update is called once per frame
        void Update()
        {
            // 添加null检查
            if (webcamManager != null && webcamManager.WebCamTexture != null)
            {
                // quadRenderer.material.mainTexture = webcamManager.WebCamTexture;
                if (OVRInput.GetDown(OVRInput.Button.One))
                {
                    TakePicture();
                }
            }
        }

        public void TakePicture()
        {
            // 添加null检查
            if (webcamManager == null || webcamManager.WebCamTexture == null)
            {
                Debug.LogWarning("WebCamTextureManager或WebCamTexture为空，无法拍照");
                return;
            }
            
            int width = webcamManager.WebCamTexture.width;
            int height = webcamManager.WebCamTexture.height;
            
            if (picture == null)
            { 
                picture = new Texture2D(width, height); 
            }
            
            Color32[] pixels = new Color32[width * height];
            webcamManager.WebCamTexture.GetPixels32(pixels);
            picture.SetPixels32(pixels);
            picture.Apply();

            // 保存图片到文件
            // SavePictureToFile();

            // 如果配置了API组件，自动触发分析
            if (apiComponent != null)
            {
                // 在这里调用API组件的分析方法
                // apiComponent.AnalyzeImage(picture);
            }

            // 添加null检查
            if (quadRenderer != null && quadRenderer.material != null)
            {
                quadRenderer.material.mainTexture = webcamManager.WebCamTexture;
            }
        }

        // 保存图片到文件
        private void SavePictureToFile()
        {
            if (picture == null)
            {
                Debug.LogWarning("没有图片可以保存");
                return;
            }
            
            // 实现保存图片的逻辑
            // byte[] bytes = picture.EncodeToPNG();
            // string path = Application.persistentDataPath + "/screenshot.png";
            // System.IO.File.WriteAllBytes(path, bytes);
            // Debug.Log("图片已保存到: " + path);
        }
      
}
}