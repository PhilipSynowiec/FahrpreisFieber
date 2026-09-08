Shader "Taxi/MinimapCircle" {
Properties { [PerRendererData] _MainTex ("Texture", 2D) = "white" {} }
SubShader {
Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
Blend SrcAlpha OneMinusSrcAlpha
Pass {
CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "UnityCG.cginc"
struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.color=v.color; o.uv=v.uv; return o; }
fixed4 frag(v2f i):SV_Target { float d=length(i.uv); i.color.a *= saturate((1-d)/max(fwidth(d),0.0001)); return i.color; }
ENDCG
}
}
}
