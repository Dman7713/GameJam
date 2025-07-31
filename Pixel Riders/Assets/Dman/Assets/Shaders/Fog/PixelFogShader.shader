Shader "Custom/PixelFogShader"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (1,1,1,1) // Main color of the fog
        _NoiseTexture ("Noise Texture (Grayscale)", 2D) = "white" {} // A grayscale noise texture (e.g., Perlin noise)
        _DitherTexture ("Dither Pattern (Grayscale)", 2D) = "white" {} // A small grayscale dither pattern (e.g., 4x4 or 8x8)

        _NoiseScale ("Noise Scale", Float) = 0.1 // Controls the size of the fog blobs
        _AnimationSpeed ("Animation Speed", Vector) = (0.1, 0.0, 0, 0) // Speed and direction of fog movement (X, Y)
        _AlphaThreshold ("Alpha Threshold", Range(0,1)) = 0.5 // Controls the overall density/opacity of the fog
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" } // Set render type to Transparent, and queue for transparency
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending
        Cull Off // Render both sides of the quad

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc" // Includes common Unity shader functions and variables (like _Time)

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_FOG_COORDS(1) // For Unity's built-in fog (optional, but good practice)
            };

            // Shader properties defined above
            fixed4 _FogColor;
            sampler2D _NoiseTexture;
            float4 _NoiseTexture_ST; // For tiling and offset of noise texture
            sampler2D _DitherTexture;
            float4 _DitherTexture_TexelSize; // For dither texture resolution
            float _NoiseScale;
            float2 _AnimationSpeed; // Renamed to float2 for direct X,Y control
            float _AlphaThreshold;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _NoiseTexture); // Apply tiling/offset from material settings
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calculate animated UV for noise texture
                // _Time.y is the total time, _AnimationSpeed.xy controls direction and speed
                float2 animatedUV = i.uv + _Time.y * _AnimationSpeed;

                // Sample noise texture
                // Divide by _NoiseScale to control the size of the noise pattern (smaller scale = larger features)
                float noiseValue = tex2D(_NoiseTexture, animatedUV / _NoiseScale).r;

                // --- Dithering for Pixel Art Transparency ---
                // Get screen-space UVs for dither pattern
                // Use _ScreenParams.xy to get screen width and height for correct pixel size
                float2 screenUV = i.vertex.xy / _ScreenParams.xy;

                // Calculate dither pattern UV based on screen pixel position
                // Use a small dither texture (e.g., 4x4 or 8x8)
                // frac ensures the pattern repeats
                float2 ditherUV = screenUV * (_ScreenParams.xy / _DitherTexture_TexelSize.xy); // Scale to dither texture pixel size
                float ditherValue = tex2D(_DitherTexture, frac(ditherUV)).r; // Changed 'fract' to 'frac'

                // Combine noise value with dither pattern
                // If noiseValue + ditherValue is greater than _AlphaThreshold, the pixel is opaque
                // Otherwise, it's transparent. This creates the dithered transparency effect.
                float alpha = step(_AlphaThreshold, noiseValue + ditherValue); // step(edge, x) returns 0 if x < edge, 1 otherwise

                // Apply fog color and calculated alpha
                fixed4 col = _FogColor;
                col.a *= alpha; // Multiply fog color's alpha by our dithered alpha

                UNITY_APPLY_FOG(i.fogCoord, col); // Apply Unity's built-in fog (optional)
                return col;
            }
            ENDCG
        }
    }
}
