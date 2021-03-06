﻿// This has to match the data in the vertex buffer.
struct VS_IN {
	float3 pos : POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};

struct VS_OUT {
	float4 pos : SV_POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};

struct PS_IN {
	float4 pos : SV_POSITION;
	float3 normal : NORMAL;
	float2 uv : TEXCOORD0;
};

struct PS_OUT {
	float4 color : SV_TARGET;
};

cbuffer constants {
	float4x4 projection;
	float4x4 view;
	float4x4 model;
};

Texture2D tex : register(t0);
SamplerState samstate : register(s0);

VS_OUT VSMain(VS_IN input) {
	VS_OUT result = (VS_OUT)0;

	// Transform the input object-space position into a screen-space position
	result.pos = mul(float4(input.pos, 1), model);
	result.pos = mul(result.pos, view);
	result.pos = mul(result.pos, projection);

	// Pass through the normal
	result.normal = input.normal;

	// Pass through the uv coordinate
	result.uv = input.uv;

	return result;
}

PS_OUT PSMain(PS_IN input) {
	PS_OUT result = (PS_OUT)0;

	// just color everything white
	//result.color = float4(1.0, 1.0, 1.0, 1.0);

	// use the texture
	//result.color = tex2D(sam, input.uv);
	result.color = tex.Sample(samstate, input.uv);

	return result;
}
