using Striked3D.Core;
using Striked3D.Core.Graphics;
using Striked3D.Core.Pipeline;
using Striked3D.Types;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.SPIRV;
using PipelineVeldrid = Veldrid.Pipeline;

namespace Striked3D.Resources
{
    public class GridMaterial3D : Material
    {
        public GridMaterial3D() : base()
        {
            this.cullMode = FaceCullMode.Back;
            this.mode = PrimitiveTopology.TriangleStrip;
            this.frontFace = FrontFace.CounterClockwise;
            this.testEnabled = false;
            this.writeEnable = false;
            this.depthStencilComparsion = ComparisonKind.GreaterEqual;
            this.blendState = BlendStateDescription.SingleAlphaBlend;
            // this.blendState.AlphaToCoverageEnabled = false;
        }

        public string _VertexCode = @"
#version 450
layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Tangent;
layout(location = 2) in vec3 Normal;
layout(location = 3) in vec4 Color;
layout(location = 4) in vec2 Uv1;
layout(location = 5) in vec2 Uv2;

layout(location = 0) out vec4 outColor;
layout(location = 1) out vec3 nearPoint;
layout(location = 2) out vec3 farPoint;
layout(location = 4) out float far;
layout(location = 5) out float near;
layout(location = 6) out mat4 fragView;
layout(location = 10) out mat4 fragProj;

layout(set = 0, binding = 0) uniform CameraBuffer {
    vec4 view_row0;
    vec4 view_row1;
    vec4 view_row2;
    vec4 view_row3;
    vec4 projection_row0;
    vec4 projection_row1;
    vec4 projection_row2;
    vec4 projection_row3;

	float farClip;
	float nearClip;
	float fovClip;
	float testClip;
};

vec3 UnprojectPoint(float x, float y, float z, mat4 view, mat4 projection) {
    mat4 viewInv = inverse(view);
    mat4 projInv = inverse(projection);
    vec4 unprojectedPoint =  viewInv * projInv * vec4(x, y, z, 1.0);
    return unprojectedPoint.xyz / unprojectedPoint.w;
}

void main()
{
    mat4 projection = mat4(projection_row0, projection_row1, projection_row2, projection_row3);
    mat4 view = mat4(view_row0, view_row1, view_row2, view_row3);

    vec3 p = Position.xyz;
	outColor = Color;

    nearPoint = UnprojectPoint(p.x, p.y, 0.0, view, projection).xyz; // unprojecting on the near plane
    farPoint = UnprojectPoint(p.x, p.y, 0.99, view, projection).xyz; // unprojecting on the far plane

    fragView = view;
    fragProj = projection;

    far = farClip;
    near = nearClip;

    gl_Position = vec4(p, 1.0); // using directly the clipped coordinates
}";

        public string _FragmentCode = @"
#version 450

layout(location = 0) in vec4 inColor;
layout(location = 1) in vec3 nearPoint;
layout(location = 2) in vec3 farPoint;

layout(location = 4) in float far;
layout(location = 5) in float near;

layout(location = 6) in mat4 fragView;
layout(location = 10) in mat4 fragProj;
layout(location = 0) out vec4 fsout_Color;

vec4 grid(vec3 fragPos3D, float scale, bool drawAxis)
{
    vec2 coord = fragPos3D.xz * scale;
    vec2 derivative = fwidth(coord);
    vec2 grid = abs(fract(coord - 0.5) - 0.5) / derivative;
    float line = min(grid.x, grid.y);
    float minimumz = min(derivative.y, 1);
    float minimumx = min(derivative.x, 1);
    vec4 color = vec4(0.2, 0.2, 0.2, 1.0 - min(line, 1.0));

    // z axis
    if(fragPos3D.x > -0.1 * minimumx && fragPos3D.x < 0.1 * minimumx)
        color.z = 1.0;

    // x axis
    if(fragPos3D.z > -0.1 * minimumz && fragPos3D.z < 0.1 * minimumz)
        color.x = 1.0;

    return color;
}

float computeDepth(vec3 pos)
{
    vec4 clip_space_pos = fragProj * fragView * vec4(pos.xyz, 1.0);

    return (clip_space_pos.z / clip_space_pos.w);
}

float computeLinearDepth(vec3 pos)
{
    vec4 clip_space_pos = fragProj * fragView * vec4(pos.xyz, 1.0);
    float clip_space_depth = (clip_space_pos.z / clip_space_pos.w) * 2.0 - 1.0; // put back between -1 and 1
    float linearDepth = (2.0 * near * far) / (far + near - clip_space_depth * (far - near)); // get linear value between 0.01 and 100

    return linearDepth / far; // normalize
}

void main()
{
    float t = -nearPoint.y / (farPoint.y - nearPoint.y);
    vec3 fragPos3D = nearPoint + t * (farPoint - nearPoint);

    float linearDepth = computeLinearDepth(fragPos3D);
    float fading = max(0, (0.5 - linearDepth));

    fsout_Color = (grid(fragPos3D, 10, true) + grid(fragPos3D, 1, true))* float(t > 0); 
    fsout_Color.a *= fading;
}";

        public override string VertexCode => _VertexCode;
        public override string FragmentCode => _FragmentCode;

        public override void Dispose()
        {
        }


        public override void BeforeDraw(IRenderer renderer)
        {
            if (this._isDirty)
            {
                builder.SetVertexLayout(new VertexLayoutDescription[] { Vertex.GetLayout() });
                builder.SetResourceLayouts(new ResourceLayout[] { renderer.Material3DLayout });
                builder.Generate(renderer, this);

                this._isDirty = false;
            }
        }
    }
}
