// Grain shader for the build-mode ghost's sand effect (see BlueprintSand.cs).
//
// Additive and unlit: overlapping grains stack up and blow past 1.0, which is what gives the dense
// parts of the cloud their glow once URP's Bloom picks them up. The grain itself is drawn
// procedurally from the billboard's UV (a soft disc), so the effect needs no sprite texture asset.
//
// Colour and alpha come from the particle's vertex colour, so the ParticleSystem's start colour and
// colour-over-lifetime drive the look. Push the start colour's HDR intensity above 1 to bloom.
Shader "CORE/Blueprint Sand"
{
    Properties
    {
        [HDR] _Tint ("Tint", Color) = (1, 1, 1, 1)
        _Hardness ("Grain Hardness", Range(0.5, 8)) = 2.5
        _MainTex ("Grain Texture (optional)", 2D) = "white" {}
        [Toggle(_USE_TEXTURE)] _UseTexture ("Use Grain Texture", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "BlueprintSand"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One // additive
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_fragment _USE_TEXTURE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float4 _MainTex_ST;
                float _Hardness;
                float _UseTexture;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Soft round grain: 1 in the middle of the billboard, 0 at its rim. Raising
                // _Hardness tightens it from a fuzzy mote into a crisp speck of sand.
                float2 p = IN.uv * 2 - 1;
                half grain = saturate(1 - dot(p, p));
                grain = pow(grain, _Hardness);

            #ifdef _USE_TEXTURE
                grain *= SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
            #endif

                half4 c = IN.color * _Tint;
                return half4(c.rgb, c.a * grain);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
