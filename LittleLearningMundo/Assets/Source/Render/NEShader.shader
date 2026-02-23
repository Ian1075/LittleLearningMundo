Shader "Custom/NCKU_SolidOutline_Thick_Final"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0, 1, 1, 1)
        _MainColor ("Base Color", Color) = (0.1, 0.1, 0.2, 1)
        _OutlineWidth ("Outline Thickness", Range(0, 0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        // Pass 1: 渲染實體加厚外殼
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            
            // 建議移除或調整 Offset，負值可能導致外框穿插到物體前方
            // Offset -1, -1 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v) {
                v2f o;
                
                o.pos = UnityObjectToClipPos(v.vertex);
                
                // 1. 將法向量轉換到視角空間
                float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal));
                
                // 2. 轉換到裁剪空間 (Projection) 以計算螢幕空間的偏移方向
                // 使用 UNITY_MATRIX_P 的 2x2 部分處理投影變換
                float2 offset = mul((float2x2)UNITY_MATRIX_P, viewNormal.xy);
                
                // 3. 修正長寬比 (Aspect Ratio)，確保線條寬度均勻
                float ratio = _ScreenParams.y / _ScreenParams.x;
                offset.x *= ratio;
                
                // 4. 歸一化並套用寬度 (乘上 pos.w 以保持透視正確)
                // 係數 0.1 是為了配合原本的參數範圍習慣
                o.pos.xy += normalize(offset) * o.pos.w * _OutlineWidth * 0.1;

                return o;
            }

            fixed4 frag () : SV_Target {
                return _OutlineColor;
            }
            ENDCG
        }

        // Pass 2: 建築本體（維持面顏色區分）
        Pass
        {
            Name "BASE"
            ZWrite On
            ZTest LEqual

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
            };

            float4 _MainColor;

            v2f vert (appdata_base v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float3 n = normalize(i.worldNormal);
                // 調整光影公式，拉大各個面的亮度對比，增強立體感
                // 頂面(y)最亮，側面(x)次之，正面(z)較暗，底面最暗
                float shading = 0.8 + n.y * 0.5 - abs(n.z) * 0.3;
                return _MainColor * shading;
            }
            ENDCG
        }
    }
}