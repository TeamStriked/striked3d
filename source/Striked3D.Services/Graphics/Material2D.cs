using Striked3D.Core;
using Striked3D.Engine.Resources;
using Striked3D.Graphics;
using Veldrid;

namespace Striked3D.Resources
{
    public class Material2D : Material
    {
        public Material2D() : base()
        {
            cullMode = FaceCullMode.None;
            frontFace = FrontFace.Clockwise;
            mode = PrimitiveTopology.TriangleList;
            blendState = BlendStateDescription.SingleAlphaBlend;
            writeEnable = false;
            testEnabled = false;
            depthClip = true;
            constantSize = Material2DInfo.GetSizeInBytes();
        }

        public string _VertexCode = @"
#version 450
layout(set = 0, binding = 0) uniform CanvasBuffer {
    vec4 screenResolution;
};

layout(location = 0) out vec4 fsin_Color;
layout(location = 1) out vec2 fsin_UV;
layout(location = 2) out float fsin_FontRange;
layout(location = 3) out float fsin_IsFont;

layout( push_constant ) uniform constants
{
	vec4 color;
	vec2 position;
	vec2 size;
    vec4 fontRegion;
	float isFont;
	float fontRange;
	float pad1;
	float pad2;
} PushConstants;

void main()
{
    vec2 pos = PushConstants.position;
    vec2 size = PushConstants.size;

    vec4 fontRegion = PushConstants.fontRegion;

    vec2 uv = vec2(fontRegion.x, fontRegion.w);

    if(gl_VertexIndex == 0)
    {
        pos = pos + size;
        uv = vec2(fontRegion.z, fontRegion.y);
    }
    if(gl_VertexIndex == 1)
    {
        pos.y = pos.y + size.y;
        uv = vec2(fontRegion.x, fontRegion.y);
    }
    if(gl_VertexIndex == 3)
    {
        pos.x = pos.x + size.x;
        uv = vec2(fontRegion.z, fontRegion.w);
    }
  
	float halfWidth = screenResolution.x / 2.0f;
	float halfHeight = screenResolution.y / 2.0f;

	pos = vec2((pos.x / halfWidth - 1.0f) , (pos.y / halfHeight - 1.0f) * -1.0f);
    gl_Position = vec4(pos, 0, 1);

    fsin_Color =  PushConstants.color;
    fsin_UV = uv;
    fsin_FontRange = PushConstants.fontRange;
    fsin_IsFont  = PushConstants.isFont;
}";

        public string _FragmentCode = @"
    #version 450

    layout(location = 0) in vec4 fsin_Color;
    layout(location = 1) in vec2 fsin_UV;
    layout(location = 2) in float fsin_FontRange;
    layout(location = 3) in float fsin_IsFont;

    layout(location = 0) out vec4 fsout_Color;

   layout(set = 1, binding = 0) uniform texture2D FontTexture;
  layout(set = 1, binding = 1) uniform sampler FontTextureSampler;

    float median(float r, float g, float b) {
	    return max(min(r, g), min(max(r, g), b));
    }

    void main()
    {
        if(fsin_IsFont >= 1.0f)
        {
       vec2 texSize = vec2(textureSize(sampler2D(FontTexture, FontTextureSampler),0));
       vec2 uvSize = vec2(fsin_UV.x / texSize.x, fsin_UV.y / texSize.y);

          vec4 colors = texture(sampler2D(FontTexture, FontTextureSampler), uvSize);
          float sigDist = median(colors.r, colors.g, colors.b) ;
           float screenPxDistance = fsin_FontRange * (sigDist - 0.5);
         float opacity = clamp(screenPxDistance + 0.5, 0.0, 1.0);

           fsout_Color = vec4(fsin_Color.rgb, opacity);
        }
        else {
            fsout_Color = fsin_Color;
       }
    }";

        public override string VertexCode => _VertexCode;
        public override string FragmentCode => _FragmentCode;

        public override void BeforeDraw(IRenderer renderer)
        {
            if (_isDirty)
            {
                //   builder.SetVertexLayout(new VertexLayoutDescription[] { Vertex2D.GetLayout() });
                builder.SetResourceLayouts(new ResourceLayout[] {
                    renderer.Material2DLayout,
                    renderer.FontAtlasLayout
                    });

                builder.Generate(renderer, this);

                _isDirty = false;

            }
        }
        public override void Dispose()
        {
        }
    }
}
