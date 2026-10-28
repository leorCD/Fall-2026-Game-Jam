//shader_type canvas_item;
//
//uniform float intensity : hint_range(0.0, 1.0) = 0.0;
//uniform float softness = 0.35;
//uniform vec4 vignette_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
//
//void fragment() {
    //float d = length(UV - 0.5) * 1.4142; // 0 at center, 1 at corners
    //float inner = mix(1.1, 0.15, intensity); // clear radius shrinks as intensity rises
    //float a = smoothstep(inner, inner + softness, d);
    //COLOR = vec4(vignette_color.rgb, a * vignette_color.a);
//}

shader_type canvas_item;

// Allows grabbing the rendered screen behind this overlay
uniform sampler2D screen_texture : hint_screen_texture, filter_linear_mipmap;

group_uniforms Strain_Controls;
// Overall intensity: 0.0 = completely invisible, 1.0 = heavy strain
uniform float intensity : hint_range(0.0, 1.5) = 0.8;
uniform float pulse_speed : hint_range(0.0, 10.0) = 3.5;
uniform float pulse_amount : hint_range(0.0, 0.5) = 0.08;

group_uniforms Vein_Appearance;
uniform vec4 vein_color : source_color = vec4(0.55, 0.03, 0.03, 1.0);
uniform vec4 vignette_tint : source_color = vec4(0.18, 0.0, 0.02, 0.6);
uniform float vein_scale : hint_range(5.0, 40.0) = 18.0;
uniform float vein_sharpness : hint_range(0.01, 0.2) = 0.045;
uniform float vignette_radius : hint_range(0.1, 1.5) = 0.65;
uniform float vignette_softness : hint_range(0.05, 1.0) = 0.45;

group_uniforms Vision_Distortion;
uniform float chromatic_aberration : hint_range(0.0, 0.03) = 0.005;

// Hash for procedural cell points
vec2 hash2(vec2 p) {
    p = vec2(dot(p, vec2(127.1, 311.7)), dot(p, vec2(269.5, 183.3)));
    return fract(sin(p) * 43758.5453123);
}

// 2D Worley Noise returning (F1, F2 - F1)
// (F2 - F1) creates the thin cellular cell borders / ridges that look like capillaries
vec2 cellular_veins(vec2 p) {
    vec2 n = floor(p);
    vec2 f = fract(p);

    float f1 = 8.0;
    float f2 = 8.0;

    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            vec2 g = vec2(float(x), float(y));
            vec2 o = hash2(n + g);

            // Add subtle warping to jitter vein paths
            vec2 delta = g + o - f;
            float d = length(delta);

            if (d < f1) {
                f2 = f1;
                f1 = d;
            } else if (d < f2) {
                f2 = d;
            }
        }
    }
    return vec2(f1, f2 - f1);
}

void fragment() {
    // 1. Screen coordinates centered at (0, 0) adjusted for aspect ratio
    vec2 uv = SCREEN_UV;
    vec2 center_uv = (uv - 0.5) * 2.0;
    // Fix aspect ratio distortion so corner distribution stays even
    center_uv.x *= (1.0 / SCREEN_PIXEL_SIZE.x) / (1.0 / SCREEN_PIXEL_SIZE.y);

    // 2. Pulse modulation (simulating heart throbbing under strain)
    float pulse = sin(TIME * pulse_speed) * pulse_amount;
    float current_intensity = clamp(intensity + (pulse * intensity), 0.0, 1.5);

    // 3. Radial Vignette Mask (concentrated around edges and corners)
    float dist = length(center_uv);
    float corner_mask = smoothstep(vignette_radius - vignette_softness, vignette_radius + vignette_softness, dist);
    corner_mask *= current_intensity;

    // 4. Vein noise coordinate distortion (makes veins twist organically toward corners)
    vec2 vein_uv = uv * vein_scale;
    // Radial stretch pushing veins outwards from center
    vein_uv += normalize(center_uv) * (dist * 1.5);

    // Evaluate Worley border distance
    vec2 cell = cellular_veins(vein_uv);
    float vein_edges = cell.y; // F2 - F1 represents distance to nearest cell wall

    // Invert and sharpen: very thin lines where cell boundaries meet
    float vein_factor = 1.0 - smoothstep(0.0, vein_sharpness, vein_edges);

    // Blend layers: heavy corner darkening + fine vascular structures
    float final_vein_alpha = vein_factor * corner_mask;
    float final_vignette_alpha = corner_mask * vignette_tint.a;

    // 5. Chromatic aberration for optical strain blur
    float ca_offset = chromatic_aberration * corner_mask;
    vec4 scene_color;
    scene_color.r = texture(screen_texture, uv + vec2(ca_offset, 0.0)).r;
    scene_color.g = texture(screen_texture, uv).g;
    scene_color.b = texture(screen_texture, uv - vec2(ca_offset, 0.0)).b;
    scene_color.a = 1.0;

    // 6. Composition
    vec4 color = scene_color;
    // Apply dark red vignette rim
    color = mix(color, vec4(vignette_tint.rgb, 1.0), final_vignette_alpha);
    // Overlay sharp red capillaries on top
    color = mix(color, vein_color, clamp(final_vein_alpha * 1.4, 0.0, 1.0));

    COLOR = color;
}