shader_type canvas_item;

uniform float intensity : hint_range(0.0, 1.0) = 0.0;
uniform float softness = 0.35;
uniform vec4 vignette_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

void fragment() {
    float d = length(UV - 0.5) * 1.4142; // 0 at center, 1 at corners
    float inner = mix(1.1, 0.15, intensity); // clear radius shrinks as intensity rises
    float a = smoothstep(inner, inner + softness, d);
    COLOR = vec4(vignette_color.rgb, a * vignette_color.a);
}