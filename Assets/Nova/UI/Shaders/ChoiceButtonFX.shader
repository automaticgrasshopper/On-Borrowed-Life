Shader "UI/ChoiceButtonFX"
{
    /*
        UI 选项按钮 Shader（科幻 · 统一风格）
        ─────────────────────────────────────────────
        三种状态层（默认全部 = 0，按钮在静态时与普通 sprite 完全一致）：

        1. 入场扫描（Reveal）
           - _RevealProgress 0→1：一道亮带从左扫向右
           - 带前 = alpha 0（不显示），带本身 = _WipeBandColor 高亮，带后 = 正常显示

        2. Hover 选中（细扫描线）
           - _HoverIntensity 0→1：叠加横向细扫描线 + 边缘高光
           - 给"被选中、被瞄准"的氛围，不抢戏

        3. 点击反馈（Glitch + 闪边）
           - _GlitchAmount 0→1：横向切片 uv 偏移
           - _EdgeFlash 0→1：边缘瞬时白闪
    */

    Properties
    {
        // ── 基础 ──
        _MainTex ("Texture", 2D) = "white" {}
        _Color   ("Tint",    Color) = (1,1,1,1)

        // ── 入场扫描 ──
        _RevealProgress ("Reveal Progress", Range(0,1)) = 1
        _WipeBandColor  ("Wipe Band Color", Color) = (0.7, 0.95, 1.0, 1)
        _WipeBandWidth  ("Wipe Band Width", Range(0.02, 0.4)) = 0.18

        // ── Hover ──
        _HoverIntensity   ("Hover Intensity",   Range(0,1)) = 0
        _ScanlineFrequency("Scanline Frequency", Range(50, 400)) = 180
        _ScanlineColor    ("Scanline Color",    Color) = (0.7, 0.95, 1.0, 1)

        // ── 点击 ──
        _GlitchAmount ("Glitch Amount", Range(0,1)) = 0
        _GlitchSlices ("Glitch Slices", Range(2, 16)) = 6
        _EdgeFlash    ("Edge Flash",    Range(0,1)) = 0
        _EdgeFlashColor("Edge Flash Color", Color) = (1,1,1,1)

        // ── Unity UI 内部 ──
        [HideInInspector] _StencilComp      ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil          ("Stencil ID",         Float) = 0
        [HideInInspector] _StencilOp        ("Stencil Operation",  Float) = 0
        [HideInInspector] _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _ColorMask        ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "ChoiceButtonFX"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            float  _RevealProgress;
            fixed4 _WipeBandColor;
            float  _WipeBandWidth;

            float  _HoverIntensity;
            float  _ScanlineFrequency;
            fixed4 _ScanlineColor;

            float  _GlitchAmount;
            float  _GlitchSlices;
            float  _EdgeFlash;
            fixed4 _EdgeFlashColor;

            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPos = IN.vertex;
                OUT.vertex   = UnityObjectToClipPos(IN.vertex);
                OUT.uv       = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color    = IN.color * _Color;
                return OUT;
            }

            // hash 用于 glitch 切片随机偏移
            float hash11(float x)
            {
                return frac(sin(x * 127.1) * 43758.5453);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.uv;

                // ── 点击 Glitch：按 uv.y 分切片做横向偏移 ──
                if (_GlitchAmount > 0.001)
                {
                    float slice = floor(uv.y * _GlitchSlices);
                    float r = hash11(slice + floor(_Time.y * 60.0)) - 0.5;
                    uv.x += r * 0.08 * _GlitchAmount;
                }

                // 基础采样
                fixed4 col = IN.color * tex2D(_MainTex, uv);

                // ── 入场扫描 ──
                // 把 progress 重映射到包含 band 宽度的范围
                float w = _WipeBandWidth;
                float bandCenter = lerp(-w, 1.0 + w, _RevealProgress);
                float distToBand = uv.x - bandCenter;

                // alpha gate：扫过的（uv.x < bandCenter - w/2）= 完全可见，
                // 没扫到的（uv.x > bandCenter + w/2）= 不可见
                float passedMask = smoothstep(-w * 0.5, w * 0.5, -distToBand);
                col.a *= passedMask;

                // 扫描带本身：高亮叠加
                float bandIntensity = 1.0 - smoothstep(0.0, w * 0.5, abs(distToBand));
                bandIntensity = saturate(bandIntensity) * (1.0 - step(0.999, _RevealProgress));
                col.rgb += _WipeBandColor.rgb * bandIntensity * 0.9;
                col.a   = max(col.a, bandIntensity * _WipeBandColor.a);

                // ── Hover：细扫描线 + 边缘高光 ──
                if (_HoverIntensity > 0.001)
                {
                    // 横向细扫描线
                    float scan = sin(uv.y * _ScanlineFrequency + _Time.y * 4.0) * 0.5 + 0.5;
                    scan = pow(scan, 6.0); // 让线变细
                    col.rgb += _ScanlineColor.rgb * scan * _HoverIntensity * 0.18;

                    // 顶/底边缘 1px 高光
                    float edgeY = min(uv.y, 1.0 - uv.y);
                    float edgeGlow = smoothstep(0.06, 0.0, edgeY);
                    col.rgb += _ScanlineColor.rgb * edgeGlow * _HoverIntensity * 0.5 * col.a;
                }

                // ── 点击边缘闪白 ──
                if (_EdgeFlash > 0.001)
                {
                    float edge = min(min(uv.x, 1.0 - uv.x), min(uv.y, 1.0 - uv.y));
                    float edgeMask = smoothstep(0.08, 0.0, edge);
                    col.rgb += _EdgeFlashColor.rgb * edgeMask * _EdgeFlash;
                }

                // Unity UI 矩形裁剪
                col.a *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);

                clip(col.a - 0.001);

                return col;
            }
            ENDCG
        }
    }
}
