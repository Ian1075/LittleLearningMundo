Shader "Custom/SimpleOutline"
{
    Properties
    {
        _MainColor ("主要顏色", Color) = (1,1,1,1)
        _MainTex ("主要貼圖", 2D) = "white" {}
        _OutlineColor ("邊框顏色", Color) = (0,1,1,1)
        _OutlineWidth ("邊框寬度", Range(0, 10)) = 2.0 // 改為像素單位感覺的數值
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }

        // 第一個 Pass：渲染邊框
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On
            ColorMask RGB

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            v2f vert (appdata v) {
                v2f o;

                // 1. 取得基礎投影位置
                o.pos = UnityObjectToClipPos(v.vertex);

                // 2. 將法線轉換到投影空間
                // 為了避免距離影響，我們在觀察空間計算方向
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 offset = TransformViewToProjection(viewNormal.xy);

                // 3. 核心修正：標準化位移方向，並考慮螢幕寬高比
                // 這樣可以確保不論距離多遠 (w 多少)，邊框在畫面上看起來粗細都一樣
                float2 aspect = float2(_ScreenParams.y / _ScreenParams.x, 1);
                
                // 這裡不再乘上 o.pos.w，而是改用一個固定的比例
                // 這樣會讓它變成「螢幕空間常數厚度」
                o.pos.xy += normalize(offset) * o.pos.w * (_OutlineWidth * 0.001) * aspect;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return _OutlineColor;
            }
            ENDCG
        }

        // 第二個 Pass：正常渲染
        Pass
        {
            Name "BASE"
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainColor;

            v2f vert (appdata v) {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return tex2D(_MainTex, i.uv) * _MainColor;
            }
            ENDCG
        }
    }
}