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
    public class Material3D : Material
    {
        public Material3D() : base()
        {
            this.cullMode = FaceCullMode.Back;
            this.frontFace = FrontFace.Clockwise;
        }

        public string _VertexCode = @"
#version 450
layout(location = 0) in vec3 Position;
layout(location = 1) in vec3 Tangent;
layout(location = 2) in vec3 Normal;
layout(location = 3) in vec4 Color;
layout(location = 4) in vec2 Uv1;
layout(location = 5) in vec2 Uv2;

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

layout(set = 1, binding = 0) uniform ModelBuffer {
    mat4 modelMatrix;
};

layout(location = 0) out vec4 fsin_Color;
void main()
{
    mat4 projection = mat4(projection_row0, projection_row1, projection_row2, projection_row3);
    mat4 view = mat4(view_row0, view_row1, view_row2, view_row3);

	vec4 worldPosition =  modelMatrix * vec4(Position.xyz, 1.0f);
    vec4 viewPosition = view * worldPosition;
    gl_Position = projection * viewPosition;

    fsin_Color = Color;
}";

        public string _FragmentCode = @"
#version 450
layout(location = 0) in vec4 fsin_Color;
layout(location = 0) out vec4 fsout_Color;

void main()
{
    fsout_Color = vec4(1,0,0,1);
}";
 
        public override string VertexCode => _VertexCode;
        public override string FragmentCode => _FragmentCode;

        public override void BeforeDraw(IRenderer renderer)
        {
            if(this._isDirty)
            {
                builder.SetVertexLayout(new VertexLayoutDescription[] { Vertex.GetLayout() });
                builder.SetResourceLayouts(new ResourceLayout[] { renderer.Material3DLayout, renderer.TransformLayout });
                builder.Generate(renderer, this);

                this._isDirty = false;
            }
        }

        public override void Dispose()
        {
        }
    }
}
