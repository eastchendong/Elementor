Shader "Hidden/ScriptBoy/ProceduralBook/PaperInstancingBuiltIn" 
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Smoothness ("Smoothness", float) = 0.2
		_Metallic ("Metallic", float) = 0
	}

    SubShader 
	{
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
		#include "UnityCG.cginc"
        #pragma surface surf Standard addshadow fullforwardshadows vertex:vert
        #pragma multi_compile_instancing
        #pragma instancing_options procedural:instancingSetup

		float4 _Color;
        sampler2D _MainTex;
		float _Smoothness;
        float _Metallic;

        struct Input 
		{
            float2 uv_MainTex : TEXCOORD;
        };
		
		#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			StructuredBuffer<float4x4> matrixBuffer;
			StructuredBuffer<float4> mainTexSTBuffer;
		#endif

        void instancingSetup()
        {
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				unity_ObjectToWorld = matrixBuffer[unity_InstanceID * 2];
				unity_WorldToObject = matrixBuffer[unity_InstanceID * 2 + 1];
			#endif
        }

		void vert (inout appdata_full v)
		{  
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				float4 ST = mainTexSTBuffer[unity_InstanceID];
				v.texcoord = float4(v.texcoord.xy * ST.xy + ST.zw, 0, 0);
			#endif
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
		{
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb * _Color.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}