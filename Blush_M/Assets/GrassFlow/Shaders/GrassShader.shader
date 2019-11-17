Shader "GrassFlow/Grass Material Shader" {
	Properties {
				[Space(15)]
		[Header(Grass Properties)]
		[HDR]_Color("Grass Color", Color) = (1,1,1,1)
		bladeHeight("Blade Height", Float) = 1.0
		bladeWidth("Blade Width", Float) = 0.05
		bladeSharp("Blade Sharpness", Float) = 0.3
		seekSun("Seek Sun", Float) = 0.6
		[Toggle(BILLBOARD)]
		_BILLBOARD("Billboard", Float) = 1
		variance("Variances (p,h,c,w)", Vector) = (0.4, 0.4, 0.4, 0.4)

		[Header(Lighting Properties)]
		_AO("AO", Float) = 0.25
		ambientCO("Ambient", Float) = 0.5
		ambientCOShadow("Shadow Ambient", Float) = 0.5
		edgeLight("Edge On Light", Float) = 0.4
		edgeLightSharp("Edge On Light Sharpness", Float) = 8

		[Space(15)]
		[Header(LOD Properties)]
		[Toggle(ALPHA_TO_MASK)]
		_ALPHA_TO_MASK("Better Transparency", Float) = 0
		widthLODscale("Width LOD Scale", Float) = 0.04
		grassFade("Grass Fade", Float) = 120
		grassFadeSharpness("Fade Sharpness", Float) = 8
		[HideInInspector]_LOD("LOD Params", Vector) = (20, 1.1, 0.2, 0.0)

		[Space(15)]
		[Header(Wind Properties)]
		[HDR]windTint("windTint", Color) = (1,1,1, 0.15)
		_noiseScale("Noise Scale", Vector) = (1,1,.7)
		_noiseSpeed("Noise Speed", Vector) = (1.5,1,0.35)
		windDir("Wind Direction", Vector) = (-0.7,-0.6,0.1)
		windDir2("Secondary Wind Direction", Vector) = (0.5,0.5,1.2)

		[Space(15)]
		[Header(Maps and Textures)]
		[Toggle(SEMI_TRANSPARENT)]
		_SEMI_TRANSPARENT("Semi Transparent Texture", Float) = 0
		alphaClip("Alpha Clip", Float) = 0.25
		numTextures("Number of Textures", Int) = 1
		_MainTex("Grass Texture", 2D) = "white"{}
		colorMap("Grass Color Map", 2D) = "white"{}
		dhfParamMap("Grass Parameter Map", 2D) = "white"{}
	}

	SubShader{

		Blend SrcAlpha OneMinusSrcAlpha
		Tags { "Queue" = "AlphaTest" "RenderType" = "Transparent" "LightMode" = "ForwardBase" }

		pass {

			AlphaToMask [_ALPHA_TO_MASK]
			Cull Off 

			CGPROGRAM

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"


			#pragma target 4.0  
			#pragma vertex vertex_shader
			#pragma geometry geometry_shader
			#pragma fragment fragment_shader

			#pragma shader_feature RENDERMODE_MESH
			#pragma shader_feature GRASS_EDITOR

			//this might look stupid but its better to use local keywords
			//but earlier unity versions dont support them so we need the global ones as fallback
			//unity ALLEGEDLY prioritizes local keywords if both are defined with same name
			//so hopefully on never version of unity it just ignores the global ones, thankfully itll still compile fine on older ones
			#pragma shader_feature_local BILLBOARD
			#pragma shader_feature_local SEMI_TRANSPARENT
			#pragma shader_feature BILLBOARD
			#pragma shader_feature SEMI_TRANSPARENT

			#pragma multi_compile_instancing
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#include "GrassStructsVars.cginc"
			#include "GrassFunc.cginc"
			#include "GrassPrograms.cginc"


			ENDCG
		}// base pass
	}




	
}
