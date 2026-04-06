shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, unshaded;

// 迁移自: GridOverlay.shader (Custom/GridOverlay_Radial)
// ⚠️ BOTTLENECK: 此 Shader 在超大地图 (10km+) 上由于 UV 精度问题
// 可能导致网格线在远处闪烁 (Z-fighting / 精度丢失)。
// 👉 优化建议: 对远处区域降低 grid_density 或用 LOD 方案。

uniform vec4 grid_color : source_color = vec4(0.0, 1.0, 0.0, 1.0);
uniform float grid_density : hint_range(1.0, 100.0) = 10.0;
uniform float line_width : hint_range(0.01, 0.2) = 0.05;
uniform float opacity : hint_range(0.0, 1.0) = 0.5;
uniform float mask_radius : hint_range(0.0, 1.0) = 0.5;
uniform float mask_softness : hint_range(0.01, 1.0) = 0.3;

void fragment() {
    // 1. 基础网格逻辑
    vec2 grid = fract(UV * grid_density);
    vec2 range_val = step(1.0 - line_width, grid) + step(grid, vec2(line_width));
    float grid_pattern = clamp(range_val.x + range_val.y, 0.0, 1.0);

    // 2. 径向遮罩逻辑 (以中心点 0.5 为圆心)
    float dist = distance(UV, vec2(0.5, 0.5));
    float mask = smoothstep(mask_radius, mask_radius - mask_softness, dist);

    ALBEDO = grid_color.rgb;
    ALPHA = grid_pattern * opacity * mask * grid_color.a;
}
