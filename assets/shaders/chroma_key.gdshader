shader_type canvas_item;

uniform vec4 key_color : source_color = vec4(0.72, 0.62, 0.80, 1.0);
uniform float threshold = 0.08;
uniform float soft = 0.02;
uniform vec4 key_color2 : source_color = vec4(0.97, 0.0, 1.0, 1.0);
uniform float threshold2 = 0.18;
uniform float soft2 = 0.04;
uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float outline_alpha = 0.9;
uniform int outline_size = 1;

void fragment() {
    vec4 c = texture(TEXTURE, UV);
    float d1 = distance(c.rgb, key_color.rgb);
    float a1 = smoothstep(threshold, threshold + soft, d1);
    float d2 = distance(c.rgb, key_color2.rgb);
    float a2 = smoothstep(threshold2, threshold2 + soft2, d2);
    float alpha = c.a * min(a1, a2);

    // Outline: if any neighbor is opaque after keying, draw outline.
    vec2 px = TEXTURE_PIXEL_SIZE * float(outline_size);
    bool edge = false;
    for (int y = -1; y <= 1; y++) {
        for (int x = -1; x <= 1; x++) {
            if (x == 0 && y == 0) {
                continue;
            }
            vec2 uv = UV + vec2(float(x), float(y)) * px;
            vec4 n = texture(TEXTURE, uv);
            float nd1 = distance(n.rgb, key_color.rgb);
            float na1 = smoothstep(threshold, threshold + soft, nd1);
            float nd2 = distance(n.rgb, key_color2.rgb);
            float na2 = smoothstep(threshold2, threshold2 + soft2, nd2);
            float nalpha = n.a * min(na1, na2);
            if (nalpha > 0.001) {
                edge = true;
            }
        }
    }

    vec4 base = vec4(c.rgb, alpha);
    vec4 outline = edge ? vec4(outline_color.rgb, outline_alpha) : vec4(0.0);
    COLOR = (alpha > 0.001) ? base : outline;
}
