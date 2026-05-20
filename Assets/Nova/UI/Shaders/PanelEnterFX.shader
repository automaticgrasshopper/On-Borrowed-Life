Shader "UI/PanelEnterFX"
{
    /*
        UI 面板入场覆盖层 Shader（科幻 HUD · 离子横扫）
        ─────────────────────────────────────────────
        参考 Iron Man HUD / Mass Effect / Cyberpunk hologram boot-up。

        驱动：_Progress 0→1
        方向：_Direction 0 = L→R（入场），1 = R→L（退场）

        视觉：
        - 一道青白亮线从左→右扫过
        - 亮线前沿是 ~16% 宽的噪声粒子带（"离子溶解"）
        - 亮线后方是一道短促的青光尾迹
        - 整体非常快（~0.15s 入场）

        与之前版本的差别：扔掉了水平切片故障 / 扫描线噪声 / 上下对称切开。
        只有一道有方向的"离子扫掠"，干净有动势。
    */

    Properties
    {
        _MainTex ("Base (keep white)", 2D) = "white" {}
        _Color   ("Cover Color", Color) = (0.01, 0.03, 0.06, 1)

        _Progress   ("Progress",    Range(0,1)) = 0
        _LayerAlpha ("Layer Alpha", Range(0,1)) = 1
        _Direction  ("Direction (0=L->R, 1=R->L)", Range(0,1)) = 0

        _OpenColor     ("Edge / Trail Color", Color) = (0.65, 0.95, 1.0, 1)
        _EdgeThickness ("Edge Thickness",     Range(0.001, 0.02)) = 0.004
        _ParticleBand  ("Particle Band Width", Range(0.05, 0.3)) = 0.16
        _NoiseScale    ("Noise Cell Density", Range(40, 400)) = 140
        _TrailLength   ("Trail Length",       Range(0.02, 0.4)) = 0.18

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
            Name "PanelEnterFX"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            float  _Progress;
            float  _LayerAlpha;
            float  _Direction;

            fixed4 _OpenColor;
            float  _EdgeThickness;
            float  _ParticleBand;
            float  _NoiseScale;
            float  _TrailLength;

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

            float hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.uv;
                float p = _Progress;

                // 方向：_Direction = 0 → t = p（L→R）；= 1 → t = 1-p（R→L）
                float t = lerp(p, 1.0 - p, _Direction);

                // 前沿 x 位置，略超出 [0,1] 以保证起始/末尾完全离屏
                float leadingX = lerp(-0.12, 1.12, t);

                // 离子噪声场：粗块状随机
                float2 cellSize = float2(_NoiseScale, _NoiseScale * 0.6);
                float2 cell = floor(uv * cellSize);
                float n = hash21(cell);

                // 每个像素的"溶解阈值"：噪声把锐利前沿打散在 _ParticleBand 宽度内
                float band = _ParticleBand;
                float threshold = (leadingX - band * 0.5) + n * band;

                // 覆盖 mask：uv.x > leadingX → 仍被覆盖（1）；uv.x < leadingX → 已露出（0）
                // 这个公式对两个方向都对称工作，因为 leadingX 本身已经被 _Direction 反转了
                float covered = step(threshold, uv.x);

                // 亮线前沿（精确在 leadingX）
                float dx = uv.x - leadingX;
                float edgeBar = 1.0 - smoothstep(0.0, _EdgeThickness, abs(dx));
                // 开局淡入 / 末尾淡出
                float edgeLife = smoothstep(0.0, 0.04, p) * (1.0 - smoothstep(0.92, 1.0, p));
                edgeBar *= edgeLife;

                // 尾迹：在已露出一侧（uv.x < leadingX）指数衰减的青色光晕
                // 入场时 = 跟在 L→R 亮线后面；退场时 = 在 R→L 亮线前方（即"将被擦除"区）
                float trailDist = leadingX - uv.x;
                float trail = exp(-max(trailDist, 0.0) / _TrailLength) * step(0.0, trailDist);
                trail *= edgeLife;

                // ── 颜色合成 ──
                fixed4 col;
                col.rgb = _Color.rgb;

                // 亮线把覆盖层底色 lerp 成青白
                col.rgb = lerp(col.rgb, _OpenColor.rgb, edgeBar);

                // 尾迹叠加（在露出区，不应叠到覆盖区）
                col.rgb += _OpenColor.rgb * trail * 0.55 * (1.0 - covered);

                // alpha 合成
                col.a = covered;
                col.a = max(col.a, edgeBar);
                col.a = max(col.a, trail * 0.30 * (1.0 - covered));

                col.a *= _LayerAlpha;
                col.a *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);

                clip(col.a - 0.001);

                return col;
            }
            ENDCG
        }
    }
}
