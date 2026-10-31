shader_type canvas_item;

// --- NES 2bpp 4-Color Sub-Palette (Index 0 is always transparent) ---
uniform bool enable_palette_swap = true;
uniform vec4 palette_color_1 : source_color = vec4(0.00, 0.25, 0.35, 1.0); // Dark / Outline
uniform vec4 palette_color_2 : source_color = vec4(0.00, 0.53, 0.58, 1.0); // Midtone
uniform vec4 palette_color_3 : source_color = vec4(0.45, 0.95, 0.85, 1.0); // Highlight

// --- NES Scanline Visibility Bitmask (216 scanlines packed into 8x 32-bit ints) ---
uniform bool enable_scanline_limit = true;
uniform int scanline_mask[8];
uniform float viewport_height = 216.0;

void fragment() {
	// 1. Evaluate Per-Scanline Visibility Limit
	if (enable_scanline_limit) {
		int scanline = clamp(int(floor(SCREEN_UV.y * viewport_height)), 0, 215);
		int bucket = scanline >> 5; // Divide by 32
		int bit_index = scanline & 31; // Modulo 32
		
		int mask_value = scanline_mask[bucket];
		if ((mask_value & (1 << bit_index)) == 0) {
			discard;
		}
	}

	// 2. Sample Base Texture
	vec4 tex_color = texture(TEXTURE, UV);
	if (tex_color.a < 0.1) {
		discard;
	}

	// 3. Map Grayscale Value to 3-Color NES Palette
	if (enable_palette_swap) {
		float luma = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
		if (luma < 0.50) {
			COLOR = vec4(palette_color_1.rgb, tex_color.a);
		} else if (luma < 0.82) {
			COLOR = vec4(palette_color_2.rgb, tex_color.a);
		} else {
			COLOR = vec4(palette_color_3.rgb, tex_color.a);
		}
	} else {
		COLOR = tex_color;
	}
}
