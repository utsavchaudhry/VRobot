Shader "WebRtcVideoChat/wrtcI420p_one_buffer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags 
		{ 
			"RenderType"="Opaque"
		}

		Pass
		{
			Cull Off
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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}


			fixed4 frag(v2f i) : SV_Target
			{
				float2 texc = i.uv;
				//top 2/3 of the image belong to y. Rest is u & v plane
				float uvoffset = (2.0f / 3.0f);
				float uvsize = (1.0f / 3.0f);
				//v uses the left side of the texture
				float voffset = 0.5f;
				float2 tex_y = float2(texc.x, texc.y * uvoffset);
				float2 tex_u = float2(texc.x * voffset, uvoffset + texc.y * uvsize);
				float2 tex_v = float2(voffset + texc.x* voffset, uvoffset + texc.y * uvsize);

				fixed y = tex2D(_MainTex, tex_y);
				fixed u = tex2D(_MainTex, tex_u);
				fixed v = tex2D(_MainTex, tex_v);
				u = u - 0.5;
				v = v - 0.5;
				fixed r = (1.164f * y + 1.596f * v);
				fixed g = (1.164f * y - 0.813f * v - 0.391f * u);
				fixed b = (1.164f * y + 2.018f * u);

				return fixed4(r, g, b, 1);

			}
			ENDCG
		}
	}
}
