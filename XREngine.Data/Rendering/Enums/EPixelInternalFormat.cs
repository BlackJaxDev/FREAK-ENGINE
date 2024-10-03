using Silk.NET.Core.Attributes;

namespace XREngine.Data.Rendering
{
    public enum ESizedInternalFormat
    {
        //Red
        R8,
        R8Snorm,
        R16,
        R16Snorm,
        R16f,
        R32f,
        R8i,
        R8ui,
        R16i,
        R16ui,
        R32i,
        R32ui,

        //Red Green
        Rg8,
        Rg8Snorm,
        Rg16,
        Rg16Snorm,
        Rg16f,
        Rg32f,
        Rg8i,
        Rg8ui,
        Rg16i,
        Rg16ui,
        Rg32i,
        Rg32ui,

        //Red Green Blue
        R3G3B2,
        Rgb4,
        Rgb5,
        Rgb8,
        Rgb8Snorm,
        Rgb10,
        Rgb12,
        Rgb16Snorm,
        Rgba2,
        Rgba4,
        Srgb8,
        Rgb16f,
        Rgb32f,
        R11fG11fB10f,
        Rgb9E5,
        Rgb8i,
        Rgb8ui,
        Rgb16i,
        Rgb16ui,
        Rgb32i,
        Rgb32ui,

        //Red Green Blue Alpha
        Rgb5A1,
        Rgba8,
        Rgba8Snorm,
        Rgb10A2,
        Rgba12,
        Rgba16,
        Srgb8Alpha8,
        Rgba16f,
        Rgba32f,
        Rgba8i,
        Rgba8ui,
        Rgba16i,
        Rgba16ui,
        Rgba32i,
        Rgba32ui,

        //Depth
        DepthComponent16,
        DepthComponent24,
        DepthComponent32f,

        //Depth Stencil
        Depth24Stencil8,
        Depth32fStencil8,

        //Stencil
        StencilIndex8,
    }

    public enum EPixelInternalFormat
    {
        StencilIndex = 6401,
        StencilIndexOes = 6401,
        DepthComponent = 6402,
        Red = 6403,
        RedExt = 6403,
        Rgb = 6407,
        Rgba = 6408,
        R3G3B2 = 10768,
        Alpha4Ext = 32827,
        Alpha8Ext = 32828,
        Alpha8Oes = 32828,
        Alpha12Ext = 32829,
        Alpha16Ext = 32830,
        Luminance4Ext = 32831,
        Luminance8Ext = 32832,
        Luminance8Oes = 32832,
        Luminance12Ext = 32833,
        Luminance16Ext = 32834,
        Luminance4Alpha4Ext = 32835,
        Luminance4Alpha4Oes = 32835,
        Luminance6Alpha2Ext = 32836,
        Luminance8Alpha8Ext = 32837,
        Luminance8Alpha8Oes = 32837,
        Luminance12Alpha4Ext = 32838,
        Luminance12Alpha12Ext = 32839,
        Luminance16Alpha16Ext = 32840,
        Intensity4Ext = 32842,
        Intensity8Ext = 32843,
        Intensity12Ext = 32844,
        Intensity16Ext = 32845,
        Rgb2Ext = 32846,
        Rgb4 = 32847,
        Rgb4Ext = 32847,
        Rgb5 = 32848,
        Rgb5Ext = 32848,
        Rgb8 = 32849,
        Rgb8Ext = 32849,
        Rgb8Oes = 32849,
        Rgb10 = 32850,
        Rgb10Ext = 32850,
        Rgb12 = 32851,
        Rgb12Ext = 32851,
        Rgb16 = 32852,
        Rgb16Ext = 32852,
        Rgba2 = 32853,
        Rgba2Ext = 32853,
        Rgba4 = 32854,
        Rgba4Ext = 32854,
        Rgba4Oes = 32854,
        Rgb5A1 = 32855,
        Rgb5A1Ext = 32855,
        Rgb5A1Oes = 32855,
        Rgba8 = 32856,
        Rgba8Ext = 32856,
        Rgba8Oes = 32856,
        [NativeName("Name", "GL_RGB10_A2")]
        Rgb10A2 = 32857,
        [NativeName("Name", "GL_RGB10_A2_EXT")]
        Rgb10A2Ext = 32857,
        [NativeName("Name", "GL_RGBA12")]
        Rgba12 = 32858,
        [NativeName("Name", "GL_RGBA12_EXT")]
        Rgba12Ext = 32858,
        [NativeName("Name", "GL_RGBA16")]
        Rgba16 = 32859,
        [NativeName("Name", "GL_RGBA16_EXT")]
        Rgba16Ext = 32859,
        [NativeName("Name", "GL_DUAL_ALPHA4_SGIS")]
        DualAlpha4Sgis = 33040,
        [NativeName("Name", "GL_DUAL_ALPHA8_SGIS")]
        DualAlpha8Sgis = 33041,
        [NativeName("Name", "GL_DUAL_ALPHA12_SGIS")]
        DualAlpha12Sgis = 33042,
        [NativeName("Name", "GL_DUAL_ALPHA16_SGIS")]
        DualAlpha16Sgis = 33043,
        [NativeName("Name", "GL_DUAL_LUMINANCE4_SGIS")]
        DualLuminance4Sgis = 33044,
        [NativeName("Name", "GL_DUAL_LUMINANCE8_SGIS")]
        DualLuminance8Sgis = 33045,
        [NativeName("Name", "GL_DUAL_LUMINANCE12_SGIS")]
        DualLuminance12Sgis = 33046,
        [NativeName("Name", "GL_DUAL_LUMINANCE16_SGIS")]
        DualLuminance16Sgis = 33047,
        [NativeName("Name", "GL_DUAL_INTENSITY4_SGIS")]
        DualIntensity4Sgis = 33048,
        [NativeName("Name", "GL_DUAL_INTENSITY8_SGIS")]
        DualIntensity8Sgis = 33049,
        [NativeName("Name", "GL_DUAL_INTENSITY12_SGIS")]
        DualIntensity12Sgis = 33050,
        [NativeName("Name", "GL_DUAL_INTENSITY16_SGIS")]
        DualIntensity16Sgis = 33051,
        [NativeName("Name", "GL_DUAL_LUMINANCE_ALPHA4_SGIS")]
        DualLuminanceAlpha4Sgis = 33052,
        [NativeName("Name", "GL_DUAL_LUMINANCE_ALPHA8_SGIS")]
        DualLuminanceAlpha8Sgis = 33053,
        [NativeName("Name", "GL_QUAD_ALPHA4_SGIS")]
        QuadAlpha4Sgis = 33054,
        [NativeName("Name", "GL_QUAD_ALPHA8_SGIS")]
        QuadAlpha8Sgis = 33055,
        [NativeName("Name", "GL_QUAD_LUMINANCE4_SGIS")]
        QuadLuminance4Sgis = 33056,
        [NativeName("Name", "GL_QUAD_LUMINANCE8_SGIS")]
        QuadLuminance8Sgis = 33057,
        [NativeName("Name", "GL_QUAD_INTENSITY4_SGIS")]
        QuadIntensity4Sgis = 33058,
        [NativeName("Name", "GL_QUAD_INTENSITY8_SGIS")]
        QuadIntensity8Sgis = 33059,
        [NativeName("Name", "GL_DEPTH_COMPONENT16")]
        DepthComponent16 = 33189,
        [NativeName("Name", "GL_DEPTH_COMPONENT16_ARB")]
        DepthComponent16Arb = 33189,
        [NativeName("Name", "GL_DEPTH_COMPONENT16_OES")]
        DepthComponent16Oes = 33189,
        [NativeName("Name", "GL_DEPTH_COMPONENT16_SGIX")]
        DepthComponent16Sgix = 33189,
        [NativeName("Name", "GL_DEPTH_COMPONENT24")]
        DepthComponent24 = 33190,
        [NativeName("Name", "GL_DEPTH_COMPONENT24_ARB")]
        DepthComponent24Arb = 33190,
        [NativeName("Name", "GL_DEPTH_COMPONENT24_OES")]
        DepthComponent24Oes = 33190,
        [NativeName("Name", "GL_DEPTH_COMPONENT24_SGIX")]
        DepthComponent24Sgix = 33190,
        [NativeName("Name", "GL_DEPTH_COMPONENT32")]
        DepthComponent32 = 33191,
        [NativeName("Name", "GL_DEPTH_COMPONENT32_ARB")]
        DepthComponent32Arb = 33191,
        [NativeName("Name", "GL_DEPTH_COMPONENT32_OES")]
        DepthComponent32Oes = 33191,
        [NativeName("Name", "GL_DEPTH_COMPONENT32_SGIX")]
        DepthComponent32Sgix = 33191,
        [NativeName("Name", "GL_COMPRESSED_RED")]
        CompressedRed = 33317,
        [NativeName("Name", "GL_COMPRESSED_RG")]
        CompressedRG = 33318,
        [NativeName("Name", "GL_RG")]
        RG = 33319,
        [NativeName("Name", "GL_R8")]
        R8 = 33321,
        [NativeName("Name", "GL_R8_EXT")]
        R8Ext = 33321,
        [NativeName("Name", "GL_R16")]
        R16 = 33322,
        [NativeName("Name", "GL_R16_EXT")]
        R16Ext = 33322,
        [NativeName("Name", "GL_RG8")]
        RG8 = 33323,
        [NativeName("Name", "GL_RG8_EXT")]
        RG8Ext = 33323,
        [NativeName("Name", "GL_RG16")]
        RG16 = 33324,
        [NativeName("Name", "GL_RG16_EXT")]
        RG16Ext = 33324,
        [NativeName("Name", "GL_R16F")]
        R16f = 33325,
        [NativeName("Name", "GL_R16F_EXT")]
        R16fExt = 33325,
        [NativeName("Name", "GL_R32F")]
        R32f = 33326,
        [NativeName("Name", "GL_R32F_EXT")]
        R32fExt = 33326,
        [NativeName("Name", "GL_RG16F")]
        RG16f = 33327,
        [NativeName("Name", "GL_RG16F_EXT")]
        RG16fExt = 33327,
        [NativeName("Name", "GL_RG32F")]
        RG32f = 33328,
        [NativeName("Name", "GL_RG32F_EXT")]
        RG32fExt = 33328,
        [NativeName("Name", "GL_R8I")]
        R8i = 33329,
        [NativeName("Name", "GL_R8UI")]
        R8ui = 33330,
        [NativeName("Name", "GL_R16I")]
        R16i = 33331,
        [NativeName("Name", "GL_R16UI")]
        R16ui = 33332,
        [NativeName("Name", "GL_R32I")]
        R32i = 33333,
        [NativeName("Name", "GL_R32UI")]
        R32ui = 33334,
        [NativeName("Name", "GL_RG8I")]
        RG8i = 33335,
        [NativeName("Name", "GL_RG8UI")]
        RG8ui = 33336,
        [NativeName("Name", "GL_RG16I")]
        RG16i = 33337,
        [NativeName("Name", "GL_RG16UI")]
        RG16ui = 33338,
        [NativeName("Name", "GL_RG32I")]
        RG32i = 33339,
        [NativeName("Name", "GL_RG32UI")]
        RG32ui = 33340,
        [NativeName("Name", "GL_COMPRESSED_RGB_S3TC_DXT1_EXT")]
        CompressedRgbS3TCDxt1Ext = 33776,
        [NativeName("Name", "GL_COMPRESSED_RGBA_S3TC_DXT1_EXT")]
        CompressedRgbaS3TCDxt1Ext = 33777,
        [NativeName("Name", "GL_COMPRESSED_RGBA_S3TC_DXT3_ANGLE")]
        CompressedRgbaS3TCDxt3Angle = 33778,
        [NativeName("Name", "GL_COMPRESSED_RGBA_S3TC_DXT3_EXT")]
        CompressedRgbaS3TCDxt3Ext = 33778,
        [NativeName("Name", "GL_COMPRESSED_RGBA_S3TC_DXT5_ANGLE")]
        CompressedRgbaS3TCDxt5Angle = 33779,
        [NativeName("Name", "GL_COMPRESSED_RGBA_S3TC_DXT5_EXT")]
        CompressedRgbaS3TCDxt5Ext = 33779,
        [NativeName("Name", "GL_COMPRESSED_RGB")]
        CompressedRgb = 34029,
        [NativeName("Name", "GL_COMPRESSED_RGBA")]
        CompressedRgba = 34030,
        [NativeName("Name", "GL_DEPTH_STENCIL")]
        DepthStencil = 34041,
        [NativeName("Name", "GL_DEPTH_STENCIL_EXT")]
        DepthStencilExt = 34041,
        [NativeName("Name", "GL_DEPTH_STENCIL_NV")]
        DepthStencilNV = 34041,
        [NativeName("Name", "GL_DEPTH_STENCIL_OES")]
        DepthStencilOes = 34041,
        [NativeName("Name", "GL_DEPTH_STENCIL_MESA")]
        DepthStencilMesa = 34640,
        [NativeName("Name", "GL_RGBA32F")]
        Rgba32f = 34836,
        [NativeName("Name", "GL_RGBA32F_ARB")]
        Rgba32fArb = 34836,
        [NativeName("Name", "GL_RGBA32F_EXT")]
        Rgba32fExt = 34836,
        [NativeName("Name", "GL_RGB32F")]
        Rgb32f = 34837,
        [NativeName("Name", "GL_RGB32F_ARB")]
        Rgb32fArb = 34837,
        [NativeName("Name", "GL_RGB32F_EXT")]
        Rgb32fExt = 34837,
        [NativeName("Name", "GL_RGBA16F")]
        Rgba16f = 34842,
        [NativeName("Name", "GL_RGBA16F_ARB")]
        Rgba16fArb = 34842,
        [NativeName("Name", "GL_RGBA16F_EXT")]
        Rgba16fExt = 34842,
        [NativeName("Name", "GL_RGB16F")]
        Rgb16f = 34843,
        [NativeName("Name", "GL_RGB16F_ARB")]
        Rgb16fArb = 34843,
        [NativeName("Name", "GL_RGB16F_EXT")]
        Rgb16fExt = 34843,
        [NativeName("Name", "GL_DEPTH24_STENCIL8")]
        Depth24Stencil8 = 35056,
        [NativeName("Name", "GL_DEPTH24_STENCIL8_EXT")]
        Depth24Stencil8Ext = 35056,
        [NativeName("Name", "GL_DEPTH24_STENCIL8_OES")]
        Depth24Stencil8Oes = 35056,
        [NativeName("Name", "GL_R11F_G11F_B10F")]
        R11fG11fB10f = 35898,
        [NativeName("Name", "GL_R11F_G11F_B10F_APPLE")]
        R11fG11fB10fApple = 35898,
        [NativeName("Name", "GL_R11F_G11F_B10F_EXT")]
        R11fG11fB10fExt = 35898,
        [NativeName("Name", "GL_RGB9_E5")]
        Rgb9E5 = 35901,
        [NativeName("Name", "GL_RGB9_E5_APPLE")]
        Rgb9E5Apple = 35901,
        [NativeName("Name", "GL_RGB9_E5_EXT")]
        Rgb9E5Ext = 35901,
        [NativeName("Name", "GL_SRGB")]
        Srgb = 35904,
        [NativeName("Name", "GL_SRGB_EXT")]
        SrgbExt = 35904,
        [NativeName("Name", "GL_SRGB8")]
        Srgb8 = 35905,
        [NativeName("Name", "GL_SRGB8_EXT")]
        Srgb8Ext = 35905,
        [NativeName("Name", "GL_SRGB8_NV")]
        Srgb8NV = 35905,
        [NativeName("Name", "GL_SRGB_ALPHA")]
        SrgbAlpha = 35906,
        [NativeName("Name", "GL_SRGB_ALPHA_EXT")]
        SrgbAlphaExt = 35906,
        [NativeName("Name", "GL_SRGB8_ALPHA8")]
        Srgb8Alpha8 = 35907,
        [NativeName("Name", "GL_SRGB8_ALPHA8_EXT")]
        Srgb8Alpha8Ext = 35907,
        [NativeName("Name", "GL_COMPRESSED_SRGB")]
        CompressedSrgb = 35912,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA")]
        CompressedSrgbAlpha = 35913,
        [NativeName("Name", "GL_COMPRESSED_SRGB_S3TC_DXT1_EXT")]
        CompressedSrgbS3TCDxt1Ext = 35916,
        [NativeName("Name", "GL_COMPRESSED_SRGB_S3TC_DXT1_NV")]
        CompressedSrgbS3TCDxt1NV = 35916,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT1_EXT")]
        CompressedSrgbAlphaS3TCDxt1Ext = 35917,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT1_NV")]
        CompressedSrgbAlphaS3TCDxt1NV = 35917,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT3_EXT")]
        CompressedSrgbAlphaS3TCDxt3Ext = 35918,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT3_NV")]
        CompressedSrgbAlphaS3TCDxt3NV = 35918,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_EXT")]
        CompressedSrgbAlphaS3TCDxt5Ext = 35919,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_S3TC_DXT5_NV")]
        CompressedSrgbAlphaS3TCDxt5NV = 35919,
        [NativeName("Name", "GL_DEPTH_COMPONENT32F")]
        DepthComponent32f = 36012,
        [NativeName("Name", "GL_DEPTH32F_STENCIL8")]
        Depth32fStencil8 = 36013,
        [NativeName("Name", "GL_STENCIL_INDEX1")]
        StencilIndex1 = 36166,
        [NativeName("Name", "GL_STENCIL_INDEX1_EXT")]
        StencilIndex1Ext = 36166,
        [NativeName("Name", "GL_STENCIL_INDEX1_OES")]
        StencilIndex1Oes = 36166,
        [NativeName("Name", "GL_STENCIL_INDEX4")]
        StencilIndex4 = 36167,
        [NativeName("Name", "GL_STENCIL_INDEX4_EXT")]
        StencilIndex4Ext = 36167,
        [NativeName("Name", "GL_STENCIL_INDEX4_OES")]
        StencilIndex4Oes = 36167,
        [NativeName("Name", "GL_STENCIL_INDEX8")]
        StencilIndex8 = 36168,
        [NativeName("Name", "GL_STENCIL_INDEX8_EXT")]
        StencilIndex8Ext = 36168,
        [NativeName("Name", "GL_STENCIL_INDEX8_OES")]
        StencilIndex8Oes = 36168,
        [NativeName("Name", "GL_STENCIL_INDEX16")]
        StencilIndex16 = 36169,
        [NativeName("Name", "GL_STENCIL_INDEX16_EXT")]
        StencilIndex16Ext = 36169,
        [NativeName("Name", "GL_RGB565_OES")]
        Rgb565Oes = 36194,
        [NativeName("Name", "GL_RGB565")]
        Rgb565 = 36194,
        [NativeName("Name", "GL_ETC1_RGB8_OES")]
        Etc1Rgb8Oes = 36196,
        [NativeName("Name", "GL_RGBA32UI")]
        Rgba32ui = 36208,
        [NativeName("Name", "GL_RGBA32UI_EXT")]
        Rgba32uiExt = 36208,
        [NativeName("Name", "GL_RGB32UI")]
        Rgb32ui = 36209,
        [NativeName("Name", "GL_RGB32UI_EXT")]
        Rgb32uiExt = 36209,
        [NativeName("Name", "GL_ALPHA32UI_EXT")]
        Alpha32uiExt = 36210,
        [NativeName("Name", "GL_INTENSITY32UI_EXT")]
        Intensity32uiExt = 36211,
        [NativeName("Name", "GL_LUMINANCE32UI_EXT")]
        Luminance32uiExt = 36212,
        [NativeName("Name", "GL_LUMINANCE_ALPHA32UI_EXT")]
        LuminanceAlpha32uiExt = 36213,
        [NativeName("Name", "GL_RGBA16UI")]
        Rgba16ui = 36214,
        [NativeName("Name", "GL_RGBA16UI_EXT")]
        Rgba16uiExt = 36214,
        [NativeName("Name", "GL_RGB16UI")]
        Rgb16ui = 36215,
        [NativeName("Name", "GL_RGB16UI_EXT")]
        Rgb16uiExt = 36215,
        [NativeName("Name", "GL_ALPHA16UI_EXT")]
        Alpha16uiExt = 36216,
        [NativeName("Name", "GL_INTENSITY16UI_EXT")]
        Intensity16uiExt = 36217,
        [NativeName("Name", "GL_LUMINANCE16UI_EXT")]
        Luminance16uiExt = 36218,
        [NativeName("Name", "GL_LUMINANCE_ALPHA16UI_EXT")]
        LuminanceAlpha16uiExt = 36219,
        [NativeName("Name", "GL_RGBA8UI")]
        Rgba8ui = 36220,
        [NativeName("Name", "GL_RGBA8UI_EXT")]
        Rgba8uiExt = 36220,
        [NativeName("Name", "GL_RGB8UI")]
        Rgb8ui = 36221,
        [NativeName("Name", "GL_RGB8UI_EXT")]
        Rgb8uiExt = 36221,
        [NativeName("Name", "GL_ALPHA8UI_EXT")]
        Alpha8uiExt = 36222,
        [NativeName("Name", "GL_INTENSITY8UI_EXT")]
        Intensity8uiExt = 36223,
        [NativeName("Name", "GL_LUMINANCE8UI_EXT")]
        Luminance8uiExt = 36224,
        [NativeName("Name", "GL_LUMINANCE_ALPHA8UI_EXT")]
        LuminanceAlpha8uiExt = 36225,
        [NativeName("Name", "GL_RGBA32I")]
        Rgba32i = 36226,
        [NativeName("Name", "GL_RGBA32I_EXT")]
        Rgba32iExt = 36226,
        [NativeName("Name", "GL_RGB32I")]
        Rgb32i = 36227,
        [NativeName("Name", "GL_RGB32I_EXT")]
        Rgb32iExt = 36227,
        [NativeName("Name", "GL_ALPHA32I_EXT")]
        Alpha32iExt = 36228,
        [NativeName("Name", "GL_INTENSITY32I_EXT")]
        Intensity32iExt = 36229,
        [NativeName("Name", "GL_LUMINANCE32I_EXT")]
        Luminance32iExt = 36230,
        [NativeName("Name", "GL_LUMINANCE_ALPHA32I_EXT")]
        LuminanceAlpha32iExt = 36231,
        [NativeName("Name", "GL_RGBA16I")]
        Rgba16i = 36232,
        [NativeName("Name", "GL_RGBA16I_EXT")]
        Rgba16iExt = 36232,
        [NativeName("Name", "GL_RGB16I")]
        Rgb16i = 36233,
        [NativeName("Name", "GL_RGB16I_EXT")]
        Rgb16iExt = 36233,
        [NativeName("Name", "GL_ALPHA16I_EXT")]
        Alpha16iExt = 36234,
        [NativeName("Name", "GL_INTENSITY16I_EXT")]
        Intensity16iExt = 36235,
        [NativeName("Name", "GL_LUMINANCE16I_EXT")]
        Luminance16iExt = 36236,
        [NativeName("Name", "GL_LUMINANCE_ALPHA16I_EXT")]
        LuminanceAlpha16iExt = 36237,
        [NativeName("Name", "GL_RGBA8I")]
        Rgba8i = 36238,
        [NativeName("Name", "GL_RGBA8I_EXT")]
        Rgba8iExt = 36238,
        [NativeName("Name", "GL_RGB8I")]
        Rgb8i = 36239,
        [NativeName("Name", "GL_RGB8I_EXT")]
        Rgb8iExt = 36239,
        [NativeName("Name", "GL_ALPHA8I_EXT")]
        Alpha8iExt = 36240,
        [NativeName("Name", "GL_INTENSITY8I_EXT")]
        Intensity8iExt = 36241,
        [NativeName("Name", "GL_LUMINANCE8I_EXT")]
        Luminance8iExt = 36242,
        [NativeName("Name", "GL_LUMINANCE_ALPHA8I_EXT")]
        LuminanceAlpha8iExt = 36243,
        [NativeName("Name", "GL_DEPTH_COMPONENT32F_NV")]
        DepthComponent32fNV = 36267,
        [NativeName("Name", "GL_DEPTH32F_STENCIL8_NV")]
        Depth32fStencil8NV = 36268,
        [NativeName("Name", "GL_COMPRESSED_RED_RGTC1")]
        CompressedRedRgtc1 = 36283,
        [NativeName("Name", "GL_COMPRESSED_RED_RGTC1_EXT")]
        CompressedRedRgtc1Ext = 36283,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_RED_RGTC1")]
        CompressedSignedRedRgtc1 = 36284,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_RED_RGTC1_EXT")]
        CompressedSignedRedRgtc1Ext = 36284,
        [NativeName("Name", "GL_COMPRESSED_RED_GREEN_RGTC2_EXT")]
        CompressedRedGreenRgtc2Ext = 36285,
        [NativeName("Name", "GL_COMPRESSED_RG_RGTC2")]
        CompressedRGRgtc2 = 36285,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_RED_GREEN_RGTC2_EXT")]
        CompressedSignedRedGreenRgtc2Ext = 36286,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_RG_RGTC2")]
        CompressedSignedRGRgtc2 = 36286,
        [NativeName("Name", "GL_COMPRESSED_RGBA_BPTC_UNORM")]
        CompressedRgbaBptcUnorm = 36492,
        [NativeName("Name", "GL_COMPRESSED_RGBA_BPTC_UNORM_ARB")]
        CompressedRgbaBptcUnormArb = 36492,
        [NativeName("Name", "GL_COMPRESSED_RGBA_BPTC_UNORM_EXT")]
        CompressedRgbaBptcUnormExt = 36492,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM")]
        CompressedSrgbAlphaBptcUnorm = 36493,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM_ARB")]
        CompressedSrgbAlphaBptcUnormArb = 36493,
        [NativeName("Name", "GL_COMPRESSED_SRGB_ALPHA_BPTC_UNORM_EXT")]
        CompressedSrgbAlphaBptcUnormExt = 36493,
        [NativeName("Name", "GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT")]
        CompressedRgbBptcSignedFloat = 36494,
        [NativeName("Name", "GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT_ARB")]
        CompressedRgbBptcSignedFloatArb = 36494,
        [NativeName("Name", "GL_COMPRESSED_RGB_BPTC_SIGNED_FLOAT_EXT")]
        CompressedRgbBptcSignedFloatExt = 36494,
        [NativeName("Name", "GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT")]
        CompressedRgbBptcUnsignedFloat = 36495,
        [NativeName("Name", "GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT_ARB")]
        CompressedRgbBptcUnsignedFloatArb = 36495,
        [NativeName("Name", "GL_COMPRESSED_RGB_BPTC_UNSIGNED_FLOAT_EXT")]
        CompressedRgbBptcUnsignedFloatExt = 36495,
        [NativeName("Name", "GL_R8_SNORM")]
        R8SNorm = 36756,
        [NativeName("Name", "GL_RG8_SNORM")]
        RG8SNorm = 36757,
        [NativeName("Name", "GL_RGB8_SNORM")]
        Rgb8SNorm = 36758,
        [NativeName("Name", "GL_RGBA8_SNORM")]
        Rgba8SNorm = 36759,
        [NativeName("Name", "GL_R16_SNORM")]
        R16SNorm = 36760,
        [NativeName("Name", "GL_R16_SNORM_EXT")]
        R16SNormExt = 36760,
        [NativeName("Name", "GL_RG16_SNORM")]
        RG16SNorm = 36761,
        [NativeName("Name", "GL_RG16_SNORM_EXT")]
        RG16SNormExt = 36761,
        [NativeName("Name", "GL_RGB16_SNORM")]
        Rgb16SNorm = 36762,
        [NativeName("Name", "GL_RGB16_SNORM_EXT")]
        Rgb16SNormExt = 36762,
        [NativeName("Name", "GL_RGBA16_SNORM")]
        Rgba16SNorm = 36763,
        [NativeName("Name", "GL_RGBA16_SNORM_EXT")]
        Rgba16SNormExt = 36763,
        [NativeName("Name", "GL_SR8_EXT")]
        SR8Ext = 36797,
        [NativeName("Name", "GL_SRG8_EXT")]
        Srg8Ext = 36798,
        [NativeName("Name", "GL_RGB10_A2UI")]
        Rgb10A2ui = 36975,
        [NativeName("Name", "GL_COMPRESSED_R11_EAC")]
        CompressedR11Eac = 37488,
        [NativeName("Name", "GL_COMPRESSED_R11_EAC_OES")]
        CompressedR11EacOes = 37488,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_R11_EAC")]
        CompressedSignedR11Eac = 37489,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_R11_EAC_OES")]
        CompressedSignedR11EacOes = 37489,
        [NativeName("Name", "GL_COMPRESSED_RG11_EAC")]
        CompressedRG11Eac = 37490,
        [NativeName("Name", "GL_COMPRESSED_RG11_EAC_OES")]
        CompressedRG11EacOes = 37490,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_RG11_EAC")]
        CompressedSignedRG11Eac = 37491,
        [NativeName("Name", "GL_COMPRESSED_SIGNED_RG11_EAC_OES")]
        CompressedSignedRG11EacOes = 37491,
        [NativeName("Name", "GL_COMPRESSED_RGB8_ETC2")]
        CompressedRgb8Etc2 = 37492,
        [NativeName("Name", "GL_COMPRESSED_RGB8_ETC2_OES")]
        CompressedRgb8Etc2Oes = 37492,
        [NativeName("Name", "GL_COMPRESSED_SRGB8_ETC2")]
        CompressedSrgb8Etc2 = 37493,
        [NativeName("Name", "GL_COMPRESSED_SRGB8_ETC2_OES")]
        CompressedSrgb8Etc2Oes = 37493,
        [NativeName("Name", "GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2")]
        CompressedRgb8PunchthroughAlpha1Etc2 = 37494,
        [NativeName("Name", "GL_COMPRESSED_RGB8_PUNCHTHROUGH_ALPHA1_ETC2_OES")]
        CompressedRgb8PunchthroughAlpha1Etc2Oes = 37494,
        CompressedSrgb8PunchthroughAlpha1Etc2 = 37495,
        CompressedSrgb8PunchthroughAlpha1Etc2Oes = 37495,
        CompressedRgba8Etc2Eac = 37496,
        CompressedRgba8Etc2EacOes = 37496,
        CompressedSrgb8Alpha8Etc2Eac = 37497,
        CompressedSrgb8Alpha8Etc2EacOes = 37497,
        CompressedRgbaAstc4x4 = 37808,
        CompressedRgbaAstc4x4Khr = 37808,
        CompressedRgbaAstc5x4 = 37809,
        CompressedRgbaAstc5x4Khr = 37809,
        CompressedRgbaAstc5x5 = 37810,
        CompressedRgbaAstc5x5Khr = 37810,
        CompressedRgbaAstc6x5 = 37811,
        CompressedRgbaAstc6x5Khr = 37811,
        CompressedRgbaAstc6x6 = 37812,
        CompressedRgbaAstc6x6Khr = 37812,
        CompressedRgbaAstc8x5 = 37813,
        CompressedRgbaAstc8x5Khr = 37813,
        CompressedRgbaAstc8x6 = 37814,
        CompressedRgbaAstc8x6Khr = 37814,
        CompressedRgbaAstc8x8 = 37815,
        CompressedRgbaAstc8x8Khr = 37815,
        CompressedRgbaAstc10x5 = 37816,
        CompressedRgbaAstc10x5Khr = 37816,
        CompressedRgbaAstc10x6 = 37817,
        CompressedRgbaAstc10x6Khr = 37817,
        CompressedRgbaAstc10x8 = 37818,
        CompressedRgbaAstc10x8Khr = 37818,
        CompressedRgbaAstc10x10 = 37819,
        CompressedRgbaAstc10x10Khr = 37819,
        CompressedRgbaAstc12x10 = 37820,
        CompressedRgbaAstc12x10Khr = 37820,
        CompressedRgbaAstc12x12 = 37821,
        CompressedRgbaAstc12x12Khr = 37821,
        CompressedRgbaAstc3x3x3Oes = 37824,
        CompressedRgbaAstc4x3x3Oes = 37825,
        CompressedRgbaAstc4x4x3Oes = 37826,
        CompressedRgbaAstc4x4x4Oes = 37827,
        CompressedRgbaAstc5x4x4Oes = 37828,
        CompressedRgbaAstc5x5x4Oes = 37829,
        CompressedRgbaAstc5x5x5Oes = 37830,
        CompressedRgbaAstc6x5x5Oes = 37831,
        CompressedRgbaAstc6x6x5Oes = 37832,
        CompressedRgbaAstc6x6x6Oes = 37833,
        CompressedSrgb8Alpha8Astc4x4 = 37840,
        CompressedSrgb8Alpha8Astc4x4Khr = 37840,
        CompressedSrgb8Alpha8Astc5x4 = 37841,
        CompressedSrgb8Alpha8Astc5x4Khr = 37841,
        CompressedSrgb8Alpha8Astc5x5 = 37842,
        CompressedSrgb8Alpha8Astc5x5Khr = 37842,
        CompressedSrgb8Alpha8Astc6x5 = 37843,
        CompressedSrgb8Alpha8Astc6x5Khr = 37843,
        CompressedSrgb8Alpha8Astc6x6 = 37844,
        CompressedSrgb8Alpha8Astc6x6Khr = 37844,
        CompressedSrgb8Alpha8Astc8x5 = 37845,
        CompressedSrgb8Alpha8Astc8x5Khr = 37845,
        CompressedSrgb8Alpha8Astc8x6 = 37846,
        CompressedSrgb8Alpha8Astc8x6Khr = 37846,
        CompressedSrgb8Alpha8Astc8x8 = 37847,
        CompressedSrgb8Alpha8Astc8x8Khr = 37847,
        CompressedSrgb8Alpha8Astc10x5 = 37848,
        CompressedSrgb8Alpha8Astc10x5Khr = 37848,
        CompressedSrgb8Alpha8Astc10x6 = 37849,
        CompressedSrgb8Alpha8Astc10x6Khr = 37849,
        CompressedSrgb8Alpha8Astc10x8 = 37850,
        CompressedSrgb8Alpha8Astc10x8Khr = 37850,
        CompressedSrgb8Alpha8Astc10x10 = 37851,
        CompressedSrgb8Alpha8Astc10x10Khr = 37851,
        CompressedSrgb8Alpha8Astc12x10 = 37852,
        CompressedSrgb8Alpha8Astc12x10Khr = 37852,
        CompressedSrgb8Alpha8Astc12x12 = 37853,
        CompressedSrgb8Alpha8Astc12x12Khr = 37853,
        CompressedSrgb8Alpha8Astc3x3x3Oes = 37856,
        CompressedSrgb8Alpha8Astc4x3x3Oes = 37857,
        CompressedSrgb8Alpha8Astc4x4x3Oes = 37858,
        CompressedSrgb8Alpha8Astc4x4x4Oes = 37859,
        CompressedSrgb8Alpha8Astc5x4x4Oes = 37860,
        CompressedSrgb8Alpha8Astc5x5x4Oes = 37861,
        CompressedSrgb8Alpha8Astc5x5x5Oes = 37862,
        CompressedSrgb8Alpha8Astc6x5x5Oes = 37863,
        CompressedSrgb8Alpha8Astc6x6x5Oes = 37864,
        CompressedSrgb8Alpha8Astc6x6x6Oes = 37865
    }
}
