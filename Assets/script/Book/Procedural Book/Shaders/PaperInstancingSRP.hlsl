#ifndef PAPER_INSTANCING_INCLUDED
#define PAPER_INSTANCING_INCLUDED

#define unity_ObjectToWorld unity_ObjectToWorld
#define unity_WorldToObject unity_WorldToObject

#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
	StructuredBuffer<float4x4> matrixBuffer;
	StructuredBuffer<float4> mainTexSTBuffer;
#endif

void instancingSetup()
{
	#if !defined(SHADERGRAPH_PREVIEW) && defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
		unity_ObjectToWorld = matrixBuffer[unity_InstanceID * 2];
		unity_WorldToObject = matrixBuffer[unity_InstanceID * 2 + 1];
	#endif
}

void instancingPosition_float(in float3 In, out float3 Out)
{
	Out = In;
}

void instancingUV_float(in float2 In, out float2 Out)
{
	#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
		float4 ST = mainTexSTBuffer[unity_InstanceID];
		Out = In * ST.xy + ST.zw;
	#else
		Out = In;
	#endif
}
#endif