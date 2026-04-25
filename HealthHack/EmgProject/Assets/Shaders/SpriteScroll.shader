Shader "Custom/SpriteScroll"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        _ScrollSpeed ("Scroll Speed (X, Y)", Vector) = (1, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #include "UnitySprites.cginc"

            float2 _ScrollSpeed;

            fixed4 frag(v2f IN) : SV_Target
            {
                // Offset the UVs over time for scrolling
                float2 uv = IN.texcoord;
                uv += _ScrollSpeed * _Time.y;
                
                // Sample the sprite texture with the new offset UVs
                fixed4 c = SampleSpriteTexture(uv) * IN.color;
                
                // Premultiplied alpha
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
