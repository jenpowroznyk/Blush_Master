// Upgrade NOTE: upgraded instancing buffer 'Props' to new syntax.


//Comment/Uncomment this depending on your indirect instancing setting
//#define INDIRECT_INSTANCING

uniform float4 _noiseScale;
uniform float4 _noiseSpeed;
uniform float3 windDir;
uniform float3 windDir2;
uniform float4 windTint;

uniform sampler2D _MainTex;
uniform float4 _MainTex_ST;

uniform float numTextures;
uniform float numTexturesPctUV;

uniform sampler2D dhfParamMap;
uniform float4 dhfParamMap_ST;
uniform sampler2D colorMap;
uniform float4 colorMap_ST;

uniform sampler3D _NoiseTex;

uniform fixed4 _Color;
uniform half alphaClip;
uniform half _AO;
uniform half bladeWidth;
uniform half bladeSharp;
uniform half bladeHeight;
uniform half ambientCO;
uniform float widthLODscale;
uniform half4 variance;
uniform half3 _LOD;
uniform half grassFade;
uniform half grassFadeSharpness;
uniform half seekSun;

#if !defined(DEFERRED)

uniform half ambientCOShadow;
uniform half edgeLight;
uniform half edgeLightSharp;

uniform fixed4 _LightColor0;

#else
uniform sampler3D _DitherMaskLOD;
uniform float _Metallic;
uniform float _Gloss;
uniform float specularMult;

struct FragmentOutput {
	float4 albedo : SV_Target0;
	float4 specular : SV_Target1;
	float4 normal : SV_Target2;
	float4 light : SV_Target3;
};

#endif


#ifdef INDIRECT_INSTANCING
uniform float4x4 objToWorldMatrix;
uniform float4x4 worldToObjMatrix;
#endif

#if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2) && !defined(SHADOWS_DEPTH)
	#define FOG_ON
#endif


#if defined(RENDERMODE_MESH)
struct v2g {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
	float4 norm : NORMAL;
};
#else

struct v2g {
	float4 pos : POSITION;
	float2 uv : TEXCOORD0;
};

uniform sampler2D terrainNormalMap;
uniform float3 terrainSize;
uniform float2 terrainChunkSize;
uniform float terrainExpansion;
uniform float terrainMapOffset;

UNITY_INSTANCING_BUFFER_START(Props)
UNITY_DEFINE_INSTANCED_PROP(float2, _chunkPos)
#define _chunkPos_arr Props
UNITY_INSTANCING_BUFFER_END(Props)

#endif

#if !defined(SHADOWS_DEPTH)
struct g2f {
	float4 pos : SV_POSITION;
	float4 col : COLOR;

	#if defined(FOG_ON)
	float3 uv : TEXCOORD0;
	#else
	float2 uv : TEXCOORD0;
	#endif

	#if defined(FORWARD_ADD)
	float3 worldPos : TEXCOORD1;
	#endif

	#if defined(DEFERRED)

	float4 normal : NORMAL;

	#else
	SHADOW_COORDS(5)
	#endif
};
#else
struct g2f {
	float4 pos : SV_POSITION;

	#if defined(SEMI_TRANSPARENT)
	float2 uv : TEXCOORD0;
	#endif
};
#endif

struct VertexData {
	float3 vertex;
	float3 normal;
	float3 color;
	float4 dhfParams; //xyz = density, height, flatten, wind str
};

struct RippleData {
	float4 pos; // w = strength
	float4 drssParams;//xyzw = decay, radius, sharpness, speed
};

struct Counter {
	uint4 val;
};

uniform StructuredBuffer<RippleData> rippleBuffer;
uniform StructuredBuffer<Counter> rippleCount;

struct DummyVert {
	float4 vertex;
};