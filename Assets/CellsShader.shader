Shader "Custom/CellsShader"
{
    Properties
    {
		_TexArray ("TexArray", 2DArray) = "" {}
		_ShadingTex ("ShadingTex", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        LOD 100

		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			// texture arrays are not available everywhere,
			// only compile shader on platforms where they are
			#pragma require 2darray

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

			float _TexScale;
			float _ShadingScale;
			float2 _Resolution;

			UNITY_DECLARE_TEX2DARRAY(_TexArray);

			sampler2D _CellsTex;
			sampler2D _ShadingTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			fixed3 calc_shading (v2f i, int cell_type) {
				int2 lb = int2((int)(tex2D(_CellsTex, i.uv + float2(-1, 0) / _Resolution * _ShadingScale).r * 255),
					           (int)(tex2D(_CellsTex, i.uv + float2(0, -1) / _Resolution * _ShadingScale).r * 255)); // left and bottom cell
				int2 rt = int2((int)(tex2D(_CellsTex, i.uv + float2(+1, 0) / _Resolution * _ShadingScale).r * 255),
					           (int)(tex2D(_CellsTex, i.uv + float2(0, +1) / _Resolution * _ShadingScale).r * 255)); // right and top cell

				float2 edge = 0;
				edge -= lb != cell_type ? 1 : 0;
				edge += rt != cell_type ? 1 : 0;

				if (any((lb != cell_type) && (rt != cell_type)))
					edge = 0;

				float2 edge_uv = (edge + 1.5) / 3.0; // (0,1,2) => (0.5, 1.5, 2.5) which are the pixel centers in the 3x3 shading tex

				return tex2D(_ShadingTex, edge_uv).rgb * 2;
			}

			fixed4 frag (v2f i) : SV_Target{
				int cell_type = (int)(tex2D(_CellsTex, i.uv).r * 255);

				if (cell_type == 0) {
					return fixed4(0, 0, 0, 0);
				}

				fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_TexArray, float3(i.uv * _Resolution / _TexScale, cell_type - 1));
				
				col.rgb *= calc_shading(i, cell_type);

				return col;
            }
            ENDCG
        }
    }
}
