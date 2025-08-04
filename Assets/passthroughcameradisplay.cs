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

        }

        // Update is called once per frame
        void Update()
        {
            if (webcamManager.WebCamTexture != null)
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
            int width = webcamManager.WebCamTexture.width;
            int height = webcamManager.WebCamTexture.height;
            if (picture == null)
            { picture = new Texture2D(width, height); }
            Color32[] pixels = new Color32[width * height];
            webcamManager.WebCamTexture.GetPixels32(pixels);
            picture.SetPixels32(pixels);
            picture.Apply();

            // 保存图片到文件
            // SavePictureToFile();

            // 如果配置了API组件，自动触发分析2

            quadRenderer.material.mainTexture = webcamManager.WebCamTexture;
        }

        // 保存图片到文件
      
}
}