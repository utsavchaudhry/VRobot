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
                #pragma multi_compile_instancing
                #pragma vertex vert
                #pragma fragment frag
                #include "UnityCG.cginc"

                struct appdata_t
                {
                    float4 vertex : POSITION;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float2 texcoord : TEXCOORD0;
                    float4 vertex : SV_POSITION;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                sampler2D _MainTex;
                float _EyeSelection;

                v2f vert(appdata_t v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.texcoord = v.texcoord;
                    return o;
                }

                half4 frag(v2f i) : SV_Target
                {
                    half2 texCoord = i.texcoord;

                    if (_EyeSelection == 0.0) // Left Eye
                    {
                        texCoord.x *= 0.5; // Use the left half of the texture
                    }
                    else if (_EyeSelection == 1.0) // Right Eye
                    {
                        texCoord.x = 0.5 + (texCoord.x * 0.5); // Use the right half of the texture
                    }

                    return tex2D(_MainTex, texCoord);
                }
                ENDCG
            }
        }
            FallBack "Diffuse"
}
