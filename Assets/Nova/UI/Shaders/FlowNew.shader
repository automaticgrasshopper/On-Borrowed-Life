// 见闻 NEW 红点流光着色器。
// 借鉴 FlowLine.shader 的 uv 流动套路，叠加一条沿 x 方向往复扫过的高光带 + 微弱呼吸亮度。
// 适用挂在 NewShowTitle.prefab 的 Image 材质上：Image 持有 NEW sprite，shader 把高光与底图相乘叠加。
Shader "Nova/UI/FlowNew"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.1, 0.1, 1)

        _GlowColor ("Glow Color", Color) = (1, 1, 1, 1)
        _GlowSpeed ("Glow Speed", Range(0, 5)) = 1.2
        _GlowWidth ("Glow Width (uv)", Range(0.02, 0.6)) = 0.25
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.4
        _BreathSpeed ("Breath Speed", Range(0, 5)) = 1.5
        _BreathAmount ("Breath Amount", Range(0, 1)) = 0.25

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
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

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _GlowColor;
            float _GlowSpeed;
            float _GlowWidth;
            float _GlowIntensity;
            float _BreathSpeed;
            float _BreathAmount;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                // 流动高光带：沿 x 方向扫过，频率 _GlowSpeed，宽度 _GlowWidth
                float t = frac(_Time.y * _GlowSpeed);
                float dist = abs(i.uv.x - t);
                float band = smoothstep(_GlowWidth, 0.0, dist);

                // 呼吸亮度（整图慢慢一明一暗）
                float breath = 1.0 + _BreathAmount * sin(_Time.y * _BreathSpeed * 6.2831853);

                fixed3 rgb = tex.rgb * breath + _GlowColor.rgb * band * _GlowIntensity * tex.a;
                fixed4 col = fixed4(rgb, tex.a);

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
