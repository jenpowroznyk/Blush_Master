#if defined(RENDERMODE_MESH)
v2g vertex_shader(v2g IN, uint inst : SV_InstanceID) {
	v2g o;
	o.pos.xyz = IN.pos.xyz;
	o.pos.w = inst;
	o.norm = IN.norm;
	o.uv = IN.uv;
	return o;
}
#else
v2g vertex_shader(v2g IN, uint inst : SV_InstanceID) {
	v2g o;
	o.pos.xyz = IN.pos.xyz;
	o.pos.w = inst;
	//o.uv = _chunkPos[0];
	o.uv = UNITY_ACCESS_INSTANCED_PROP(_chunkPos_arr, _chunkPos);
	return o;
}
#endif

//v2g vertex_depth(v2g IN, uint inst : SV_InstanceID) {
//	v2g o;
//	o.pos.xyz = IN.pos.xyz;
//	o.pos.w = inst;
//	//return 0;
//	return o;
//}


#ifdef LOWER_QUALITY 
[maxvertexcount(3)]
#else 
[maxvertexcount(4)]
#endif

void geometry_shader(triangle v2g IN[3], inout TriangleStream<g2f> outStream, uint primId : SV_PrimitiveID) {

	g2f o;

#define vd1 IN[0]
#define vd2 IN[1]
#define vd3 IN[2]
	//#define GetRndUV


#ifndef RENDERMODE_MESH
	//TERRAIN MODE
	VertexData lvd; //terrain sample data
	rngfloat rndSeed = rngfloat(primId, primId * 2.4378952, vd1.pos.w);
	//float2 rndUV = frac(float2(rndm(rndSeed), rndm(rndSeed)) / rndm(rndSeed));
	//float chunkVariance = rndm(rndSeed) - 0.5;
	float2 rndUV = float2(rndm(rndSeed), rndm(rndSeed));
	GetHeightmapData(lvd, rndUV, vd1.uv, rndSeed);

	UNITY_BRANCH
	if (rndm(rndSeed) > lvd.dhfParams.x) {
		return;
	}

	float3 toTri = mul(grassToWorld, float4(lvd.vertex, 1)) - _WorldSpaceCameraPos;

#else
	//MESH MODE
	rngfloat rndSeed = rngfloat(primId, primId * 2.4378952, vd1.pos.w);

	VertexData lvd; //lerped vertex data
	float2 rndUV = float2(rndm(rndSeed), rndm(rndSeed));
	LerpVertData(lvd, rndUV, vd1, vd2, vd3, rndSeed);

	UNITY_BRANCH
	if (rndm(rndSeed) > lvd.dhfParams.x) {
		return;
	}

	float3 toTri = mul(grassToWorld, float4(lvd.vertex.xyz, 1)) - _WorldSpaceCameraPos;
#endif

#ifdef DEFERRED
	//variate length to avoid artifacting in dithering
	toTri *= rndm(rndSeed) * 0.75 + 1.0;
#endif

	//calculate fade alpha
	float camDist = rsqrt(dot(toTri, toTri));
	half alphaBlendo = saturate(pow(camDist * grassFade, grassFadeSharpness));

	UNITY_BRANCH
		if (alphaBlendo < 0.05) return;


#if !defined(BILLBOARD)
	//float3 camRight = float3(1, rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f);
	float3 camRight = normalize(float3(rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f));
#else
	float3 camRight = mul(worldToGrass, mul((float3x3)unity_CameraToWorld, float3(1, rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f)));
#endif

	camDist = 1.0 + widthLODscale / camDist;

	half noiseSamp;


	half3 widthMod = camRight * (1.0 + lvd.dhfParams.z * 0.5) * bladeWidth * Variate(rndm(rndSeed), variance.w) * camDist;

	float3 tV = TP_Vert(lvd, camDist, rndSeed, noiseSamp);
	float3 lV = lvd.vertex - widthMod;
	float3 rV = lvd.vertex + widthMod;

	ApplyRipples(tV);

	#if !defined(SHADOWS_DEPTH) || defined(SEMI_TRANSPARENT)
	float uvXL = int(rndm(rndSeed) * numTextures) * numTexturesPctUV;
	float uvXR = uvXL + numTexturesPctUV;
	#else
	static float uvXL = 0;
	static float uvXR = 1;
	#endif

#if !defined(SHADOWS_DEPTH) && !defined(FORWARD_ADD) && !defined(DEFERRED)

	half diffuseCO = 1.0 - ambientCO;
	half3 lightDirection = _WorldSpaceLightPos0;
	//half lightAmnt = saturate(1.0 - lightDirection.y);
	half shade = ambientCO + diffuseCO * saturate(dot(lightDirection, lvd.normal) + 0.25);
	shade *= 1 + saturate(dot(lightDirection, cross(cross(lightDirection, float3(0, -edgeLightSharp, 0)), lvd.normal)) - (edgeLightSharp - edgeLight));
	//shade = saturate(dot(camFwd, lightDirection));
	//shade *= saturate(0.5 + lightDirection.y);

	//noiseSamp = 0.8f + noiseSamp * 0.25f;
	noiseSamp = noiseSamp * 1.5 - 0.5;
	float3 windTintAdd = float3(1, 1, 1) + windTint.rgb * windTint.a * noiseSamp;


	//TOP Vert - no AO on this one
	//ShadeVert(o, tV, lvd, shade, noiseSamp, alphaBlendo, rndSeed, float2(0.5, 1.0));
	float4 bladeCol = float4(lvd.color * pow(Variate(rndm(rndSeed), variance.z), 0.4), alphaBlendo);

	float3 sh9 = max(0, ShadeSH9(float4(UnityObjectToWorldNormal(lvd.normal), 1)) * ambientCO);
	bladeCol.rgb = bladeCol.rgb * windTintAdd * _LightColor0.rgb * shade + bladeCol.rgb * sh9;

	CHECK_PAINT_HIGHLIGHT;

	o.col = bladeCol;

#endif

	#if defined(FORWARD_ADD)
	float4 bladeCol = float4(lvd.color, alphaBlendo);
	o.col = bladeCol;
	#endif

	#if defined(DEFERRED)

	noiseSamp = noiseSamp * 1.5 - 0.5;
	float3 windTintAdd = float3(1, 1, 1) + windTint.rgb * windTint.a * noiseSamp;
	float4 bladeCol = float4(lvd.color * pow(Variate(rndm(rndSeed), variance.z), 0.4) * windTintAdd, alphaBlendo);
	o.col = bladeCol;

	float3 worldGroundNormal = UnityObjectToWorldNormal(lvd.normal) * 0.5 + 0.5;
	//o.normal.xyz = worldGroundNormal;

	//weird alternative normals situation give more variation and rough grass normals
	o.normal.xyz = lerp(worldGroundNormal, normalize(UnityObjectToWorldNormal(tV - lvd.vertex)) * 0.5 + 0.5, 0.3);

	o.normal.w = 1.0 * specularMult;
	#endif



	//this is really dumb but the way that the built in shadow transfer function works is that it expects you to call it in a vertex shader
	//with a struct "v" that has a float4 "vertex" just already defined, well we dont have that so just do this as a dumb workaround
	DummyVert v;

	//Top left Vert
	v.vertex = float4(tV - widthMod * bladeSharp, 1.0);
	float3 worldPos = mul(grassToWorld, v.vertex);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXL, 1.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o);
	outStream.Append(o);


	//Top right Vert
	v.vertex = float4(tV + widthMod * bladeSharp, 1.0);
	worldPos = mul(grassToWorld, v.vertex);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXR, 1.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o);
	outStream.Append(o);


#if !defined(SHADOWS_DEPTH)
	//Reduce AO a bit on flattened grass and variate AO a smidge
	float aoValue = lerp(lvd.dhfParams.z + rndm(rndSeed) * 0.2 + _AO, 1.0, noiseSamp * lvd.dhfParams.x * 0.35 + (1.0 - lvd.dhfParams.x) * 0.5);
	bladeCol *= aoValue; // apply AO
	//bladeCol *= lvd.dhfParams.x * 0.5 + 0.5; // reduce AO based on grass density;
	bladeCol.a = alphaBlendo;

	CHECK_PAINT_HIGHLIGHT;

	o.col = bladeCol;
#endif


#if defined(DEFERRED)
	o.normal.w = aoValue;
#endif


	//BL Vert
	v.vertex = float4(lV, 1.0);
	worldPos = mul(grassToWorld, v.vertex);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXL, 0.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o);
	outStream.Append(o);

	//BR Vert
	v.vertex = float4(rV, 1.0);
	worldPos = mul(grassToWorld, v.vertex);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXR, 0.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o);
	outStream.Append(o);



	//outStream.RestartStrip();
}


#if !defined(SHADOWS_DEPTH)

#ifndef DEFERRED
fixed4 fragment_shader(g2f i) : SV_Target{
#else
FragmentOutput fragment_shader(g2f i) {
#endif


	#if defined(DEFERRED)
	//half alphaRef = tex3D(_DitherMaskLOD, float3((i.worldPos.xy + i.worldPos.z)*2, i.col.a*0.9375 + 0.0001)).a;
	half alphaRef = tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, i.col.a*0.9375 + 0.0001)).a;
	clip(alphaRef - 0.01);
	#endif

	fixed4 col = tex2D(_MainTex, i.uv);

#if defined(SEMI_TRANSPARENT)
	clip(col.a - alphaClip);
#endif
	
#ifdef FORWARD_ADD
	UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
	col.rgb *= _LightColor0.rgb * atten;

#elif defined(SHADOWS_SCREEN)
	col.rgb *= ambientCOShadow + (1.0 - ambientCOShadow) * SHADOW_ATTENUATION(i);
#endif

	col = saturate(col * i.col);
	UNITY_APPLY_FOG(i.uv.z, col);

#if defined(DEFERRED)

	FragmentOutput deferredData;

	half3 specular;
	half specularMonochrome;
	half3 diffuseColor = DiffuseAndSpecularFromMetallic(col.rgb, _Metallic, specular, specularMonochrome);
	
	deferredData.albedo.rgb = diffuseColor; //albedo	
	deferredData.albedo.a = _Metallic; //occulusion
	
	
	deferredData.specular.rgb = specular * i.normal.w; //specular tint
	deferredData.specular.a = _Gloss * i.normal.w; //shinyness

	deferredData.normal = float4(i.normal.xyz, 1);

	//indirect lighting
	float3 sh9 = max(0, ShadeSH9(float4(i.normal.xyz, 1)) * ambientCO);
	deferredData.light.rgb = diffuseColor * sh9;

	#if !defined(UNITY_HDR_ON)
	deferredData.light.rgb = exp2(-deferredData.light.rgb);
	#endif

	deferredData.light.a = 0;

	return deferredData;

#else
	return col;
#endif

}
#endif

#if defined(SEMI_TRANSPARENT)
void fragment_depth(g2f i) {
	fixed4 col = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex));
	clip(col.a - alphaClip);
}
#else
void fragment_depth() {
}
#endif