﻿#version 400
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

#pragma fragment

layout (location = 0) in Vector4 Color;
layout (location = 0) out Vector4 FragColor;

void main()
{
    FragColor = Color;
}