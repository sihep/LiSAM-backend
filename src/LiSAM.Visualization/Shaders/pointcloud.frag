#version 330 core

in vec3 pointColor;
out vec4 fragColor;

void main()
{
    vec2 fromCenter = gl_PointCoord * 2.0 - 1.0;
    if (dot(fromCenter, fromCenter) > 1.0)
        discard;

    fragColor = vec4(pointColor, 1.0);
}
