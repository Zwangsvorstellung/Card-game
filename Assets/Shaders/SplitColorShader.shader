Shader "Custom/SplitColorShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TopColor ("Top Half Color", Color) = (0.5, 0.5, 1, 0.4)
        _BottomColor ("Bottom Half Color", Color) = (1, 0.5, 0.5, 0.4)
        _BlendStrength ("Blend Strength", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "PreviewType"="Plane"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
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
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _TopColor;
            float4 _BottomColor;
            float _BlendStrength;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Échantillonner la texture originale
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // Déterminer si on est dans la moitié gauche ou droite
                float isLeftHalf = step(0.5, i.uv.x);
                
                // Appliquer la couleur appropriée selon la position
                float4 overlayColor = lerp(_BottomColor, _TopColor, isLeftHalf);
                
                // Mélanger avec la couleur originale
                float4 finalColor = lerp(col, overlayColor, _BlendStrength * overlayColor.a);
                finalColor.a = col.a; // Garder l'alpha original
                
                return finalColor;
            }
            ENDCG
        }
    }
} 