#version 330 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aColor;
layout(location = 2) in float aSize;

out vec3 pointColor;

uniform mat4 projectionMatrix;
uniform mat4 viewMatrix;

void main()
{
    pointColor = aColor;
    gl_PointSize = aSize;
    gl_Position = vec4(aPosition, 1.0) * viewMatrix * projectionMatrix;
}
