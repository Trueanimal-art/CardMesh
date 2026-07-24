Shader "CardMesh/UI/Light Frame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _UvRect ("Sprite UV Rect", Vector) = (0,0,1,1)
        _JewelOverlay ("Jewel Overlay", Float) = 0
        _DividerOverlay ("Divider Overlay", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _UvRect;
            float _JewelOverlay;
            float _DividerOverlay;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            float CompressEdge(float value, float sourceEdge)
            {
                const float targetEdge = 0.06;
                if (value < targetEdge)
                    return value * sourceEdge / targetEdge;
                if (value > 1.0 - targetEdge)
                    return 1.0 - sourceEdge + (value - (1.0 - targetEdge)) * sourceEdge / targetEdge;
                return sourceEdge + (value - targetEdge) * (1.0 - 2.0 * sourceEdge) / (1.0 - 2.0 * targetEdge);
            }

            float CompressCenterDecoration(float value)
            {
                const float sourceHalf = 0.125;
                const float targetHalf = 0.075;
                if (value < 0.5 - targetHalf)
                    return value * (0.5 - sourceHalf) / (0.5 - targetHalf);
                if (value > 0.5 + targetHalf)
                    return 0.5 + sourceHalf + (value - (0.5 + targetHalf)) * (0.5 - sourceHalf) / (0.5 - targetHalf);
                return 0.5 + (value - 0.5) * sourceHalf / targetHalf;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 localUv = (input.texcoord - _UvRect.xy) / _UvRect.zw;

                if (_JewelOverlay > 0.5)
                {
                    half4 jewelColor = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                    float jewelShape = saturate(1.0
                        - abs(localUv.x - 0.5) / 0.50
                        - abs(localUv.y - 0.5) / 0.50);
                    jewelColor.a *= smoothstep(0.0, 0.055, jewelShape);

                    float jewelCore = saturate(1.0
                        - abs(localUv.x - 0.5) / 0.24
                        - abs(localUv.y - 0.5) / 0.24);
                    jewelColor.rgb *= 1.0 + jewelCore * 0.12;
                    jewelColor.rgb += fixed3(0.008, 0.045, 0.015) * jewelCore;

                    #ifdef UNITY_UI_CLIP_RECT
                    jewelColor.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                    #endif
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(jewelColor.a - 0.001);
                    #endif
                    return jewelColor;
                }

                if (_DividerOverlay > 0.5)
                {
                    const float dividerCenter = 0.523;

                    half4 dividerColor = (tex2D(_MainTex, input.texcoord) + _TextureSampleAdd) * input.color;
                    float sourceDominant = max(dividerColor.r, dividerColor.g);

                    float lineMask = (1.0 - smoothstep(
                        0.012,
                        0.038,
                        abs(localUv.y - dividerCenter)))
                        * smoothstep(0.20, 0.38, sourceDominant - dividerColor.b * 0.20);

                    float diamondShape = saturate(1.0
                        - abs(localUv.x - 0.5) / 0.048
                        - abs(localUv.y - dividerCenter) / 0.43);
                    float diamondMask = smoothstep(0.0, 0.06, diamondShape);

                    // The divider fragment baked into the source atlas has a complete
                    // lower point, but its matching upper point is flattened in the
                    // pixels themselves. Reconstruct only the upper half of the center
                    // diamond from the intact lower half. Keeping the repair inside the
                    // existing diamond mask preserves the line, size and placement.
                    float2 mirroredLocalUv = float2(
                        localUv.x,
                        dividerCenter - (localUv.y - dividerCenter));
                    float2 mirroredUv = _UvRect.xy + mirroredLocalUv * _UvRect.zw;
                    half4 mirroredDividerColor =
                        (tex2D(_MainTex, mirroredUv) + _TextureSampleAdd) * input.color;
                    float upperDiamondRepair = step(dividerCenter + 0.002, localUv.y)
                        * diamondMask;
                    dividerColor = lerp(
                        dividerColor,
                        mirroredDividerColor,
                        upperDiamondRepair);

                    float dominant = max(dividerColor.r, dividerColor.g);

                    float centerDetails = (1.0 - smoothstep(0.065, 0.16, abs(localUv.x - 0.5)))
                        * smoothstep(0.28, 0.48, dominant)
                        * smoothstep(0.08, 0.22, dominant - dividerColor.b);

                    float ornamentMask = max(lineMask, max(diamondMask, centerDetails));
                    dividerColor.a *= ornamentMask;

                    float emeraldCore = saturate(
                        (dividerColor.g - max(dividerColor.r, dividerColor.b)) * 3.0);
                    dividerColor.rgb *= 1.0 + emeraldCore * 0.10;

                    #ifdef UNITY_UI_CLIP_RECT
                    dividerColor.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                    #endif
                    #ifdef UNITY_UI_ALPHACLIP
                    clip(dividerColor.a - 0.001);
                    #endif
                    return dividerColor;
                }

                float2 decoratedUv = localUv;
                const float targetEdge = 0.06;

                if (localUv.x < targetEdge || localUv.x > 1.0 - targetEdge)
                    decoratedUv.y = CompressCenterDecoration(decoratedUv.y);
                if (localUv.y < targetEdge || localUv.y > 1.0 - targetEdge)
                    decoratedUv.x = CompressCenterDecoration(decoratedUv.x);

                decoratedUv.x = CompressEdge(decoratedUv.x, 0.13);
                decoratedUv.y = CompressEdge(decoratedUv.y, 0.18);

                float2 sourceUv = _UvRect.xy + decoratedUv * _UvRect.zw;
                half4 color = (tex2D(_MainTex, sourceUv) + _TextureSampleAdd) * input.color;

                // The source frame contains a divider which is rendered again as
                // a standalone sprite. Remove only the ornament pixels here. The
                // previous implementation flattened the whole horizontal band to
                // one texture row, producing the visible striped/blurred rectangle.
                const float lineSource = 0.659;
                float dividerBand = 1.0 - smoothstep(0.010, 0.072, abs(decoratedUv.y - lineSource));
                float centerZone = 1.0 - smoothstep(0.055, 0.115, abs(decoratedUv.x - 0.5));
                float goldPixel = smoothstep(0.10, 0.30, color.r - color.b)
                    * smoothstep(0.18, 0.48, color.r);
                float emeraldPixel = centerZone
                    * smoothstep(0.035, 0.16, color.g - max(color.r, color.b))
                    * smoothstep(0.16, 0.42, color.g);
                float ornamentPixel = dividerBand * saturate(max(goldPixel, emeraldPixel));

                float backgroundOffset = _UvRect.w * 0.082;
                half4 backgroundAbove = tex2D(_MainTex, sourceUv + float2(0.0, backgroundOffset)) + _TextureSampleAdd;
                half4 backgroundBelow = tex2D(_MainTex, sourceUv - float2(0.0, backgroundOffset)) + _TextureSampleAdd;
                half4 restoredBackground = (backgroundAbove + backgroundBelow) * 0.5 * input.color;
                color = lerp(color, restoredBackground, ornamentPixel);

                // Preserve the source geometry. These masks only restore emerald
                // light and gold contrast; they never resample or scale a jewel.
                float upperJewel = saturate(1.0
                    - abs(localUv.x - 0.5) / 0.060
                    - abs(localUv.y - 0.971) / 0.055);
                float lowerJewel = saturate(1.0
                    - abs(localUv.x - 0.5) / 0.060
                    - abs(localUv.y - 0.043) / 0.055);
                float jewelMask = max(upperJewel, lowerJewel);

                float emeraldCore = saturate(1.0
                    - abs(localUv.x - 0.5) / 0.026
                    - min(abs(localUv.y - 0.971), abs(localUv.y - 0.043)) / 0.022);
                color.rgb *= 1.0 + emeraldCore * 0.15;
                color.rgb += fixed3(0.010, 0.050, 0.017) * emeraldCore;

                float goldSignal = saturate((color.r - color.b) * 2.1) * jewelMask;
                color.rgb = lerp(
                    color.rgb,
                    color.rgb * fixed3(1.12, 1.07, 0.95),
                    goldSignal * 0.44);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
