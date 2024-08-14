Shader "Custom/SplitTextureShader"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _EyeSelection("Eye Selection", Float) = 0 // 0 for Left, 1 for Right
    }
        SubShader
        {
            Tags {"RenderType" = "Opaque"}
            LOD 200

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float2 texcoord : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                };

                sampler2D _MainTex;
                float _EyeSelection; // 0 = Left, 1 = Right

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = v.texcoord;
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    half2 texCoord = i.texcoord;

                    if (_EyeSelection == 0.0) // Left Eye
                    {
                        texCoord.x *= 2.0; // Stretch the left half over the full UV space
                    }
                    else if (_EyeSelection == 1.0) // Right Eye
                    {
                        texCoord.x = (texCoord.x - 0.5) * 2.0; // Stretch the right half over the full UV space
                    }

                    return tex2D(_MainTex, texCoord);
                }
                ENDCG
            }
        }
            FallBack "Diffuse"
}
