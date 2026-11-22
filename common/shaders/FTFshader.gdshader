shader_type canvas_item;

uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;

uniform float aberration_strength : hint_range(0.0, 5.0) = 3.0;
uniform float blur_strength : hint_range(0.0, 3.0) = 2.0;
uniform float noise_strength : hint_range(0.0, 0.3) = 0.048;
uniform float vignette_strength : hint_range(0.0, 1.0) = 0.78;
uniform float scanline_strength : hint_range(0.0, 1.0) = 0.016;
uniform float distortion_strength : hint_range(0.0, 1.0) = 0.1;

group_uniforms Ghosting_Controls;
uniform float ghost_strength : hint_range(0.0, 0.5) = 0.12;
uniform float ghost_flicker_speed : hint_range(0.5, 30.0) = 14.0;
// 0.0 = smooth drifting ghost, 1.0 = erratic jumping/glitching
uniform float ghost_jitter : hint_range(0.0, 1.0) = 0.75;
// Minimum baseline multiplier so the ghost doesn't disappear completely
uniform float ghost_min_strength : hint_range(0.0, 1.0) = 0.15;
// Base horizontal trail distance
uniform float ghost_distance : hint_range(0.0, 0.05) = 0.012;

uniform float flicker_strength : hint_range(0.0, 0.1) = 0.005;

float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

float rand_1d(float t) {
    return fract(sin(t * 12.9898) * 43758.5453);
}

vec3 blur_sample(vec2 uv) {
    vec2 texel = 1.0 / vec2(textureSize(screen_tex, 0));
    vec3 col = texture(screen_tex, uv).rgb;
    col += texture(screen_tex, uv + vec2(texel.x, 0.0)).rgb;
    col += texture(screen_tex, uv - vec2(texel.x, 0.0)).rgb;
    col += texture(screen_tex, uv + vec2(0.0, texel.y)).rgb;
    col += texture(screen_tex, uv - vec2(0.0, texel.y)).rgb;
    return col / 5.0;
}

void fragment() {
    vec2 uv = SCREEN_UV;

    // Scanline & jitter distortion
    float wave = sin(uv.y * 200.0 + TIME * 10.0) * 0.002 * distortion_strength;
    uv.x += wave;
    uv.y += (rand(vec2(uv.y * 100.0, TIME * 10.0)) - 0.5) * 0.002;

    // Chromatic aberration
    vec2 offset = (uv - 0.5) * 0.004 * aberration_strength;
    float r = texture(screen_tex, uv + offset).r;
    float g = texture(screen_tex, uv).g;
    float b = texture(screen_tex, uv - offset).b;
    vec3 col = vec3(r, g, b);

    // Blur pass
    col = mix(col, blur_sample(uv), clamp(blur_strength, 0.0, 1.0));

    // Dynamic randomized ghosting
    float t = TIME * ghost_flicker_speed;

    // Smooth wave blending multiple non-harmonic frequencies
    float smooth_wave = sin(t * 0.8) * 0.35 + sin(t * 1.9) * 0.35 + sin(t * 3.1) * 0.3;
    smooth_wave = clamp(smooth_wave * 0.5 + 0.5, 0.0, 1.0);

    // Stepped random jumps and sudden signal spikes
    float stepped_t = floor(t * 1.2);
    float twitch = rand_1d(stepped_t);
    float burst = step(0.85, rand_1d(stepped_t * 0.43)) * rand_1d(t);

    float combined_jitter = mix(smooth_wave, twitch, ghost_jitter);
    combined_jitter = clamp(combined_jitter + burst * 0.5, 0.0, 1.0);

    float current_ghost_mult = mix(ghost_min_strength, 1.0, combined_jitter);
    float current_ghost_strength = ghost_strength * current_ghost_mult;

    // Slightly wobble the trail distance along with the intensity
    vec2 ghost_uv = uv;
    ghost_uv.x -= ghost_distance * (0.8 + current_ghost_mult * 0.4);

    vec3 ghost_col = texture(screen_tex, ghost_uv).rgb;
    col = mix(col, ghost_col, current_ghost_strength);

    // Scanlines
    float scanline = sin(uv.y * 800.0 + sin(TIME * 5.0) * 10.0) * 0.5 + 0.5;
    col *= 1.0 - scanline * scanline_strength;

    // Film grain / noise
    float noise = (rand(uv * TIME * 50.0) - 0.5) * 2.0 * noise_strength;
    col += noise;

    // Vignette
    float dist = distance(uv, vec2(0.5));
    float vignette = smoothstep(0.6, 0.9, dist);
    col *= mix(1.0, 1.0 - vignette, vignette_strength);

    // Screen flicker
    float flicker = 1.0 + flicker_strength * (sin(TIME * 50.0) + (rand(vec2(TIME * 10.0, 0.0)) - 0.5));
    col *= flicker;

    COLOR = vec4(col, 1.0);
}