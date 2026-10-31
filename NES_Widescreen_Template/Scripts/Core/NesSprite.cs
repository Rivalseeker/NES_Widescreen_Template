using Godot;

namespace NesTemplate.Core;

public partial class NesSprite : Sprite2D
{
    public static readonly Color[][] PresetPalettes =
    {
        // 0: Mega Cyan / Teal
        new[] { new Color("004058"), new Color("008894"), new Color("74f2d8") },
        // 1: Crimson / Gold
        new[] { new Color("500000"), new Color("b81c10"), new Color("f8d878") },
        // 2: Forest Emerald
        new[] { new Color("003c14"), new Color("009432"), new Color("b8f878") },
        // 3: Royal Violet / Rose
        new[] { new Color("2c004c"), new Color("842094"), new Color("f8a4c0") },
        // 4: Monochrome Game Gear / GB style
        new[] { new Color("1b2a1b"), new Color("4c704c"), new Color("c4e8c4") }
    };

    public int CurrentPaletteIndex { get; private set; } = 0;
    public int[] ScanlineMaskBuffer { get; } = new int[8];

    private ShaderMaterial _shaderMaterial = null!;
    private static Shader? _sharedShader;

    public override void _Ready()
    {
        _sharedShader ??= GD.Load<Shader>("res://Shaders/nes_sprite.gdshader");

        _shaderMaterial = new ShaderMaterial
        {
            Shader = _sharedShader
        };
        Material = _shaderMaterial;

        for (int i = 0; i < 8; i++)
            ScanlineMaskBuffer[i] = -1;

        ApplyPresetPalette(CurrentPaletteIndex);
        PushScanlineMaskToShader();

        NesSpriteManager.Instance?.RegisterSprite(this);
    }

    public override void _ExitTree()
    {
        NesSpriteManager.Instance?.UnregisterSprite(this);
    }

    public void SetCustomPalette(Color color1, Color color2, Color color3)
    {
        _shaderMaterial.SetShaderParameter("palette_color_1", color1);
        _shaderMaterial.SetShaderParameter("palette_color_2", color2);
        _shaderMaterial.SetShaderParameter("palette_color_3", color3);
    }

    public void ApplyPresetPalette(int paletteIndex)
    {
        CurrentPaletteIndex = Mathf.PosMod(paletteIndex, PresetPalettes.Length);
        Color[] pal = PresetPalettes[CurrentPaletteIndex];
        SetCustomPalette(pal[0], pal[1], pal[2]);
    }

    public void CyclePalette(int step = 1)
    {
        ApplyPresetPalette(CurrentPaletteIndex + step);
    }

    public void SetScanlineLimitEnabled(bool enabled)
    {
        _shaderMaterial.SetShaderParameter("enable_scanline_limit", enabled);
    }

    public void PushScanlineMaskToShader()
    {
        _shaderMaterial.SetShaderParameter("scanline_mask", ScanlineMaskBuffer);
    }

    public Rect2 GetScreenBoundingRect(Transform2D canvasTransform)
    {
        if (Texture == null)
            return new Rect2(GlobalPosition, Vector2.Zero);

        Vector2 size = Texture.GetSize() * GlobalScale.Abs();
        Vector2 topLeftWorld = Centered
            ? GlobalPosition - (size * 0.5f) + Offset
            : GlobalPosition + Offset;

        Vector2 screenPos = canvasTransform * topLeftWorld;
        return new Rect2(screenPos, size);
    }
}
