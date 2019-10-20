Shader "Custom/CellsShader"
{
    Properties
    {
		_MaterialTexArray ("MaterialTexArray", 2DArray) = "" {}
		_ShadingTexArray ("ShadingTex", 2DArray) = "" {}
		_DisplacementTex ("DisplacementTex", 2D) = "" {}
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

			float2 _TexScale;
			float2 _ShadingScale;

			float _MaterialTextureIndecies[32];
			float _MaterialShadingModes[32];

			UNITY_DECLARE_TEX2DARRAY(_MaterialTexArray);
			UNITY_DECLARE_TEX2DARRAY(_ShadingTexArray);

			sampler2D _CellsTex;
			sampler2D _DisplacementTex;

			float2 _DisplacementScale;
			float2 _DisplacementTexOffset;
			float _DisplacementStrength;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

			float4 calc_shading (v2f i, int mat_id, float shading_mode) {
				int2 lb = int2((int)(tex2D(_CellsTex, i.uv + float2(-1, 0) * _ShadingScale).r * 255),
					           (int)(tex2D(_CellsTex, i.uv + float2(0, -1) * _ShadingScale).r * 255)); // left and bottom cell
				int2 rt = int2((int)(tex2D(_CellsTex, i.uv + float2(+1, 0) * _ShadingScale).r * 255),
					           (int)(tex2D(_CellsTex, i.uv + float2(0, +1) * _ShadingScale).r * 255)); // right and top cell

				float2 edge = 0;
				edge -= lb != mat_id ? 1 : 0;
				edge += rt != mat_id ? 1 : 0;

				if (shading_mode == 0.0 && any((lb != mat_id) && (rt != mat_id)))
					edge = 0;

				float2 edge_uv = (edge + 1.5) / 3.0; // (0,1,2) => (0.5, 1.5, 2.5) which are the pixel centers in the 3x3 shading tex

				return UNITY_SAMPLE_TEX2DARRAY(_ShadingTexArray, float3(edge_uv, shading_mode)) * fixed4(2,2,2,1); // grey is neural color to so that white boosts color
			}

			float4 frag (v2f i) : SV_Target{
				int mat_id = (int)(tex2D(_CellsTex, i.uv).r * 255);

				float tex_index = _MaterialTextureIndecies[mat_id];
				float shading_mode = _MaterialShadingModes[mat_id];

				clip(tex_index); // Invisible

				float displacement = tex2D(_DisplacementTex, i.uv * _DisplacementScale + _DisplacementTexOffset) * _DisplacementStrength;

				float4 col = UNITY_SAMPLE_TEX2DARRAY(_MaterialTexArray, float3(i.uv * _TexScale + displacement, tex_index));
				
				col *= calc_shading(i, mat_id, shading_mode);

				return col;
            }
            ENDCG
        }
    }
}
