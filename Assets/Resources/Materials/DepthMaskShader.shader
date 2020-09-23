Shader "DepthMask"
{
    SubShader
    {
        Tags {"Queue" = "Geometry"}		// default value
        Pass
        {
            ZTest LEqual	// default value
            ColorMask 0    
        }
    }
}