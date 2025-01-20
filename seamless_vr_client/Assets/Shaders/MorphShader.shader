// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader"Custom/MorphShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "" {}
        _ProjectTex("Project Texture", 2D) = "" {}
        _CamPos("Camera Position", Vector) = (0, 0, 0, 0)
        _CamNormal("Camera Normal", Vector) = (0, 0, 0)
        _IStep("Interpolate I Step", Float) = 1
        _NStep("Interpoalte N Step", Float) = 10
        _F("Camera Quad Distance", Float) = 1.0
        _Mode("Morph Mode", Integer) = 0  // 0:non-morph 1:tex-morph, 2:proj-shader
        _ShadowMap("Shadow Map", 2D) = ""
        //_WorldToCamMat
        //_BlendCamProjectionMat
    }

    SubShader
    {
        ZWrite On

        Tags { "RenderType"="Opaque"}
            
        CGPROGRAM
        #pragma target 4.0
        #pragma surface surf StandardDefaultGI fullforwardshadows vertex:vert addshadow
        #include "UnityPBSLighting.cginc"

        float4 _CamPos;
        float3 _CamNormal;
        float _IStep;
        float _NStep;
        float _F;
        int _Mode;
        float4x4 _WorldToCamMat;
        float4x4 _UniScaleMat;
        float4x4 _UniTransMat;
        float4x4 _BlendCamProjectionMat;
        sampler2D _MainTex;
        sampler2D _ProjectTex;

        struct Input
        {
            float2 uv_MainTex;
            float4 v_morph;
            float4 v_pre_morph;
            float3 v_normal;
        };

        // inout is all the mesh vertex value
        void vert(inout appdata_full v, out Input o)
        {
             
            UNITY_INITIALIZE_OUTPUT(Input, o);

            if (_Mode == 1 || _Mode == 2)
            {
                // morph algorithm
                float4 v_in_world = mul(unity_ObjectToWorld, v.vertex);
                float4 trans_v_in_world = mul(_UniScaleMat, mul(_UniTransMat, mul(unity_ObjectToWorld, v.vertex)));
                float dist = distance(trans_v_in_world.xyz, _CamPos.xyz);
                float4x4 MV = mul(_WorldToCamMat, unity_ObjectToWorld);
                float4 trans_v_in_view = mul(_WorldToCamMat, trans_v_in_world);
                float z = abs(trans_v_in_view.z);
                float r = dist * _F / z;
                float3 dir = normalize(trans_v_in_world.xyz - _CamPos.xyz);
                float3 v1 = _CamPos.xyz + r * dir;
                float3 v0 = trans_v_in_world.xyz;
                float3 v2 = v_in_world;

                // set vertex animation
                float3 step_dir = normalize(v1 - v2);
                float dN = distance(v1, v2);
                float step_d = dN / _NStep;
                float3 vi = v2 + step_dir * step_d * _IStep;
                float4 v_morph = float4(vi, 1.0);
                o.v_morph = v_morph;
                o.v_pre_morph = float4(v2, 1.0);
                
                v.vertex = mul(unity_WorldToObject, v_morph);
            }
        }
    
        inline half4 LightingStandardDefaultGI(SurfaceOutputStandard s, half3 viewDir, UnityGI gi)
        {
            return LightingStandard(s, viewDir, gi);
        }
    
        inline void LightingStandardDefaultGI_GI(
                    SurfaceOutputStandard s,
                    UnityGIInput data,
                    inout UnityGI gi)
        {
            LightingStandard_GI(s, data, gi);
        }
   
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
            o.Smoothness = 0.0f;
            o.Metallic = 0.0f;
        }
        ENDCG
    }
}