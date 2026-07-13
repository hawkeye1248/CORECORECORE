// Blueprint preview shader: draws only the edges and corners of a block's bounding box, over an
// almost-invisible fill. Used by the build-mode ghost (see BuildingSystem.CreateGhost).
//
// It measures the fragment's distance to the mesh's object-space bounding box rather than to the
// triangles, so the flat faces of a cube stay clean: a real wireframe shader (barycentric or
// geometry-shader based) would also draw the diagonal where each face is split into two triangles.
// The trade-off is that it only reads as a wireframe on box-shaped meshes; on anything else it
// outlines the bounding box, not the silhouette.
//
// The bounds arrive per renderer through a MaterialPropertyBlock, because every buildable uses the
// same cube mesh at a different scale.
Shader "CORE/Blueprint Wireframe"
{
    Properties
    {
        [HDR] _EdgeColor ("Edge Colour", Color) = (0.25, 0.8, 1, 1)
        [HDR] _CornerColor ("Corner Colour", Color) = (0.85, 0.98, 1, 1)
        _FillColor ("Fill Colour", Color) = (0.2, 0.6, 1, 0.06)

        _Thickness ("Edge Thickness (world units)", Range(0.001, 0.5)) = 0.05
        _CornerLength ("Corner Length (world units, 0 = draw whole edges)", Range(0, 5)) = 0
        _CornerSize ("Corner Highlight Size (world units)", Range(0, 5)) = 0.35
        _Softness ("Edge Softness", Range(0.5, 4)) = 1

        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("Depth Test", Float) = 4 // LEqual

        // Snapping parks the ghost flush against a surface, which makes the two coplanar faces
        // z-fight. Bias the ghost's depth towards the camera so it always wins, the same trick
        // decals use. Push these further from zero if fighting reappears at long draw distances.
        _DepthBiasFactor ("Depth Bias (slope factor)", Float) = -1
        _DepthBiasUnits ("Depth Bias (constant units)", Float) = -1

        [HideInInspector] _BoundsMin ("Bounds Min (object space)", Vector) = (-0.5, -0.5, -0.5, 0)
        [HideInInspector] _BoundsMax ("Bounds Max (object space)", Vector) = (0.5, 0.5, 0.5, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "BlueprintWireframe"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Offset [_DepthBiasFactor], [_DepthBiasUnits]
            Cull Off // draw the far side of the box too, so the ghost reads as a see-through cage

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
                half4 _CornerColor;
                half4 _FillColor;
                float _Thickness;
                float _CornerLength;
                float _CornerSize;
                float _Softness;
                float _ZTest;
                float _DepthBiasFactor;
                float _DepthBiasUnits;
                float4 _BoundsMin;
                float4 _BoundsMax;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Distance to the nearest bounding plane on each of the three axes.
                float3 d = min(IN.positionOS - _BoundsMin.xyz, _BoundsMax.xyz - IN.positionOS);

                // Convert to world units, so a stretched cube still gets even-width lines.
                float3 scale = float3(
                    length(unity_ObjectToWorld._m00_m10_m20),
                    length(unity_ObjectToWorld._m01_m11_m21),
                    length(unity_ObjectToWorld._m02_m12_m22));
                d *= scale;

                // Sort the three. Every visible fragment sits on a face, so the smallest distance is
                // ~0 (that face). The middle one is then the distance to the nearest edge of the
                // face, and the largest is the distance along that edge to the nearest corner.
                float lo = min(min(d.x, d.y), d.z);
                float hi = max(max(d.x, d.y), d.z);
                float mid = d.x + d.y + d.z - lo - hi;

                float aaMid = fwidth(mid) * _Softness;
                float aaHi = fwidth(hi) * _Softness;

                float edge = 1 - smoothstep(max(_Thickness - aaMid, 0), _Thickness + aaMid, mid);

                // Corner-bracket mode: keep only the stretch of each edge within _CornerLength of a
                // corner. Zero draws the full edge, i.e. a complete cage.
                if (_CornerLength > 0)
                    edge *= 1 - smoothstep(max(_CornerLength - aaHi, 0), _CornerLength + aaHi, hi);

                float corner = 1 - smoothstep(0, max(_CornerSize, 1e-4), hi);
                half4 wire = lerp(_EdgeColor, _CornerColor, corner);

                return half4(lerp(_FillColor.rgb, wire.rgb, edge),
                             lerp(_FillColor.a, wire.a, edge));
            }
            ENDHLSL
        }
    }

    Fallback Off
}
