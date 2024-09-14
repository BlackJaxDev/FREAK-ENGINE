#version 450
layout (location = 0) out vec4 OutColor;
uniform vec4 MatColor;
void main() { OutColor = MatColor; }