#version 450
#extension GL_ARB_separate_shader_objects : enable

layout (location = 0) in vec3 vPosition;
layout (location = 1) in vec3 vNormal;
layout (location = 2) in vec3 vColor;

//push constants block
layout( push_constant ) uniform constants
{
 mat4 projection;
 mat4 view;
 float near;
 float far;
 float test;
 float test2;
 mat4 model;
} PushConstants;

layout(location = 0) out vec3 outColor;
layout(location = 1) out vec3 nearPoint;
layout(location = 2) out vec3 farPoint;
layout(location = 4) out float far;
layout(location = 5) out float near;
layout(location = 6) out mat4 fragView;
layout(location = 10) out mat4 fragProj;


vec3 UnprojectPoint(float x, float y, float z, mat4 view, mat4 projection) {
    mat4 viewInv = inverse(view);
    mat4 projInv = inverse(projection);
    vec4 unprojectedPoint =  viewInv * projInv * vec4(x, y, z, 1.0);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

void main() 
{	
    vec3 p = vPosition;
	outColor = vColor;

    nearPoint = UnprojectPoint(p.x, p.y, 0.0, PushConstants.view, PushConstants.projection).xyz; // unprojecting on the near plane
    farPoint = UnprojectPoint(p.x, p.y, 0.99, PushConstants.view, PushConstants.projection).xyz; // unprojecting on the far plane

    fragView = PushConstants.view;
    fragProj = PushConstants.projection;

    far = PushConstants.far;
    near = PushConstants.near;

    gl_Position = vec4(p, 1.0); // using directly the clipped coordinates
}