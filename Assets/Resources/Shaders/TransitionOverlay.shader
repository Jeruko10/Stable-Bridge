Shader "Custom/TransitionOverlay"
{
    Properties
    {
        _MainTex ("Mask Sprite", 2D) = "white" {}
        _Scale ("Scale", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+100" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Scale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centered = i.uv - 0.5;
                float2 maskUV = centered / max(_Scale, 0.0001) + 0.5;

                float inBounds = step(0, maskUV.x) * step(maskUV.x, 1) *
                                 step(0, maskUV.y) * step(maskUV.y, 1);

                float maskAlpha = inBounds * tex2D(_MainTex, maskUV).a;

                // transparent inside the mask (game shows through), black outside
                return fixed4(0, 0, 0, 1 - maskAlpha);
            }
            ENDCG
        }
    }
}
