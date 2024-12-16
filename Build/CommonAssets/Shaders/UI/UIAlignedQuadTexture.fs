#version 450

layout(location = 0) in vec2 vTexCoord;
layout(location = 0) out vec4 fragColor;

uniform sampler2D Texture0;
uniform vec2 TextureSize;
uniform vec2 CornerSize;

void main()
{
    vec2 texCoord = vTexCoord * TextureSize;
    vec2 cornerSize = CornerSize;

    if (texCoord.x < cornerSize.x)
    {
        if (texCoord.y < cornerSize.y)
        {
            // Bottom-left corner
            fragColor = texture(Texture0, texCoord / cornerSize);
        }
        else if (texCoord.y > TextureSize.y - cornerSize.y)
        {
            // Top-left corner
            fragColor = texture(Texture0, vec2(texCoord.x, texCoord.y - (TextureSize.y - cornerSize.y)) / cornerSize);
        }
        else
        {
            // Left edge
            fragColor = texture(Texture0, vec2(texCoord.x, cornerSize.y + (texCoord.y - cornerSize.y) / (TextureSize.y - 2.0 * cornerSize.y)));
        }
    }
    else if (texCoord.x > TextureSize.x - cornerSize.x)
    {
        if (texCoord.y < cornerSize.y)
        {
            // Bottom-right corner
            fragColor = texture(Texture0, vec2(texCoord.x - (TextureSize.x - cornerSize.x), texCoord.y) / cornerSize);
        }
        else if (texCoord.y > TextureSize.y - cornerSize.y)
        {
            // Top-right corner
            fragColor = texture(Texture0, (texCoord - (TextureSize - cornerSize)) / cornerSize);
        }
        else
        {
            // Right edge
            fragColor = texture(Texture0, vec2(texCoord.x - (TextureSize.x - cornerSize.x), cornerSize.y + (texCoord.y - cornerSize.y) / (uTextureSize.y - 2.0 * cornerSize.y)));
        }
    }
    else
    {
        if (texCoord.y < cornerSize.y)
        {
            // Bottom edge
            fragColor = texture(Texture0, vec2(cornerSize.x + (texCoord.x - cornerSize.x) / (TextureSize.x - 2.0 * cornerSize.x), texCoord.y) / cornerSize);
        }
        else if (texCoord.y > TextureSize.y - cornerSize.y)
        {
            // Top edge
            fragColor = texture(Texture0, vec2(cornerSize.x + (texCoord.x - cornerSize.x) / (TextureSize.x - 2.0 * cornerSize.x), texCoord.y - (TextureSize.y - cornerSize.y)) / cornerSize);
        }
        else
        {
            // Center
            fragColor = texture(Texture0, vec2(cornerSize.x + (texCoord.x - cornerSize.x) / (TextureSize.x - 2.0 * cornerSize.x), cornerSize.y + (texCoord.y - cornerSize.y) / (TextureSize.y - 2.0 * cornerSize.y)));
        }
    }
}