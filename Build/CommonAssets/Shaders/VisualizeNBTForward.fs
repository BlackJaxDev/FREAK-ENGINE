#version 450
layout (location = 0) out vec4 OutColor;
layout (location = 4) in vec4 FragColor;
void main() { OutColor = FragColor; }