// Dissolve shader for placed building blocks (see BlockDissolve.cs). Lit, not unlit: a placed block
// has to keep the lighting and textures it will have once it's solid, or it would visibly pop the
// moment the effect ends and its real material comes back.
//
// It carries URP Lit's property names (_BaseMap, _BaseColor, _BumpMap, _Metallic, ...) on purpose:
// BlockDissolve.cs clones each of the block's real materials and swaps only the shader, so every
// value the artist authored survives the swap and the block looks like itself while it materialises.
//
// _DissolveAmount is 1 when the block is entirely gone and 0 when it is fully solid. Animate it 1 -> 0
// to build a block up out of nothing, or 0 -> 1 to erode one away.
Shader "CORE/Block Dissolve"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor ("Base Colour", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        [HDR] _EmissionColor ("Emission", Color) = (0, 0, 0, 0)
        _Metallic ("Metallic", Range(0, 1)) = 0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5

        [Header(Dissolve)]
        _DissolveAmount ("Dissolve Amount (1 = gone, 0 = solid)", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Float) = 6
        [HDR] _EdgeColor ("Burn Edge Colour", Color) = (2.5, 1.2, 0.3, 1)
        _EdgeWidth ("Burn Edge Width", Range(0.001, 0.5)) = 0.08

        // 0 = the block reassembles as random speckle, 1 = a clean bottom-up sweep.
        _Sweep ("Bottom-Up Sweep (0 = speckle, 1 = clean sweep)", Range(0, 1)) = 0.6

        // World-space Y range of the block, so the sweep knows where its bottom and top are.
        // BlockDissolve.cs fills these in from the renderer's bounds.
        [HideInInspector] _SweepMinY ("Sweep Min Y", Float) = 0
        [HideInInspector] _SweepMaxY ("Sweep Max Y", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest" // it clips, so it belongs with the cutout geometry
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half4 _EdgeColor;
            float _BumpScale;
            float _Metallic;
            float _Smoothness;
            float _DissolveAmount;
            float _NoiseScale;
            float _EdgeWidth;
            float _Sweep;
            float _SweepMinY;
            float _SweepMaxY;
        CBUFFER_END

        // --- Procedural 3D value noise, so the effect needs no noise texture asset. ---
        float DissolveHash(float3 p)
        {
            p = frac(p * 0.3183099 + 0.1);
            p *= 17.0;
            return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
        }

        float DissolveNoise(float3 x)
        {
            float3 i = floor(x);
            float3 f = frac(x);
            f = f * f * (3.0 - 2.0 * f); // smoothstep the cell interpolation

            return lerp(
                lerp(lerp(DissolveHash(i + float3(0, 0, 0)), DissolveHash(i + float3(1, 0, 0)), f.x),
                     lerp(DissolveHash(i + float3(0, 1, 0)), DissolveHash(i + float3(1, 1, 0)), f.x), f.y),
                lerp(lerp(DissolveHash(i + float3(0, 0, 1)), DissolveHash(i + float3(1, 0, 1)), f.x),
                     lerp(DissolveHash(i + float3(0, 1, 1)), DissolveHash(i + float3(1, 1, 1)), f.x), f.y),
                f.z);
        }

        /// How eroded this point is, 0..1. Points with a LOW value come back first as _DissolveAmount
        /// falls, so mixing in the height gradient makes the block build itself from the ground up.
        float DissolveField(float3 positionWS)
        {
            float noise = DissolveNoise(positionWS * _NoiseScale) * 0.65
                        + DissolveNoise(positionWS * _NoiseScale * 2.3) * 0.35;

            float height = saturate((positionWS.y - _SweepMinY) / max(_SweepMaxY - _SweepMinY, 1e-4));
            return lerp(noise, height, saturate(_Sweep));
        }

        /// Clips away whatever hasn't materialised yet, and reports how close the survivor is to the
        /// cut so the caller can set the burn edge alight.
        float DissolveClip(float3 positionWS)
        {
            float d = DissolveField(positionWS) - _DissolveAmount;
            clip(d);

            float edge = 1.0 - saturate(d / max(_EdgeWidth, 1e-4));
            // Fade the rim out as the block finishes, or the last sliver would keep glowing forever.
            return edge * saturate(_DissolveAmount * 6.0);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // Shading toggles copied off the block's real material by BlockDissolve.cs, so the
            // dissolve lights the block the same way its own material does. These are multi_compile,
            // not shader_feature (which is what URP Lit uses): the materials that switch them on are
            // built at runtime, and shader_feature variants nothing in the project references get
            // stripped from player builds.
            #pragma multi_compile_local_fragment _ _SPECULARHIGHLIGHTS_OFF
            #pragma multi_compile_local_fragment _ _ENVIRONMENTREFLECTIONS_OFF

            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);   SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);   SAMPLER(sampler_BumpMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
                float fogCoord : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS = nrm.normalWS;
                OUT.tangentWS = float4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogCoord = ComputeFogFactor(pos.positionCS.z);

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST, OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half burn = DissolveClip(IN.positionWS);

                SurfaceData surface = (SurfaceData)0;
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                surface.albedo = albedo.rgb;
                surface.alpha = 1;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;
                surface.normalTS = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                // The burn rim is emissive, so it blooms along with the rest of the sand.
                surface.emission = _EmissionColor.rgb + _EdgeColor.rgb * burn;

                float tangentSign = IN.tangentWS.w;
                float3 bitangent = tangentSign * cross(IN.normalWS.xyz, IN.tangentWS.xyz);

                InputData input = (InputData)0;
                input.positionWS = IN.positionWS;
                input.normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(
                    surface.normalTS, half3x3(IN.tangentWS.xyz, bitangent, IN.normalWS.xyz)));
                input.viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(IN.positionWS));
                input.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                input.fogCoord = IN.fogCoord;
                input.bakedGI = SAMPLE_GI(IN.lightmapUV, IN.vertexSH, input.normalWS);
                input.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                input.shadowMask = SAMPLE_SHADOWMASK(IN.lightmapUV);

                half4 color = UniversalFragmentPBR(input, surface);
                color.rgb = MixFog(color.rgb, input.fogCoord);
                return color;
            }
            ENDHLSL
        }

        // Clipped shadows, so a half-materialised block casts a half shadow instead of a solid one.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif

                float4 positionCS = TransformWorldToHClip(
                    ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif

                OUT.positionCS = positionCS;
                OUT.positionWS = positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                DissolveClip(IN.positionWS);
                return 0;
            }
            ENDHLSL
        }

        // Keeps the block correct in the depth prepass / depth texture while it is clipped.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                DissolveClip(IN.positionWS);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
