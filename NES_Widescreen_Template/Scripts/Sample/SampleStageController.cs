using Godot;
using System.Collections.Generic;
using NesTemplate.Core;

namespace NesTemplate.Sample;

public partial class SampleStageController : Node2D
{
    private SamplePlayer _player = null!;
    private readonly List<PatrolDummy> _dummies = new();
    private Label _hudLabel = null!;

    private bool _prevScaleKey;
    private bool _prevPaletteKey;
    private bool _prevEnemyPaletteKey;
    private bool _prevFlickerKey;

    public override void _Ready()
    {
        BuildStageGeometry();
        SpawnPlayer();
        SpawnScanlineTestBots();
        BuildDebugHud();
    }

    public override void _Process(double delta)
    {
        HandleDebugHotkeys();
        UpdateHudText();
    }

    private void HandleDebugHotkeys()
    {
        // [F1] Cycle Window Integer Scale (1x .. MaxScale)
        bool scaleKey = Input.IsKeyPressed(Key.F1);
        if (scaleKey && !_prevScaleKey)
        {
            DisplayManager.Instance.CycleWindowScale(1);
        }
        _prevScaleKey = scaleKey;

        // [F2] Cycle NES Scanline Limit & Flicker Mode
        bool flickerKey = Input.IsKeyPressed(Key.F2);
        if (flickerKey && !_prevFlickerKey)
        {
            NesSpriteManager.Instance.CycleLimitMode();
        }
        _prevFlickerKey = flickerKey;

        // [P] Cycle Player Sprite Palette
        bool paletteKey = Input.IsKeyPressed(Key.P);
        if (paletteKey && !_prevPaletteKey)
        {
            _player.Sprite.CyclePalette(1);
        }
        _prevPaletteKey = paletteKey;

        // [O] Cycle All Patrol Bot Palettes
        bool enemyPaletteKey = Input.IsKeyPressed(Key.O);
        if (enemyPaletteKey && !_prevEnemyPaletteKey)
        {
            foreach (PatrolDummy bot in _dummies)
            {
                bot.Sprite.CyclePalette(1);
            }
        }
        _prevEnemyPaletteKey = enemyPaletteKey;
    }

    private void BuildStageGeometry()
    {
        ImageTexture brickTex = PixelArtGenerator.CreateBrickTexture();

        // Floor (24 tiles across = 384px at y = 200)
        CreatePlatformRow(brickTex, startX: 0, count: 24, y: 200, paletteIndex: 1);

        // Left & Right Walls
        for (int y = 40; y < 200; y += 16)
        {
            CreatePlatformRow(brickTex, startX: 0, count: 1, y: y, paletteIndex: 1);
            CreatePlatformRow(brickTex, startX: 368, count: 1, y: y, paletteIndex: 1);
        }

        // Mid-air Platforms for jumping through scanline bands
        CreatePlatformRow(brickTex, startX: 48, count: 6, y: 152, paletteIndex: 2);
        CreatePlatformRow(brickTex, startX: 240, count: 6, y: 152, paletteIndex: 2);
        CreatePlatformRow(brickTex, startX: 128, count: 8, y: 104, paletteIndex: 3);
    }

    private void CreatePlatformRow(ImageTexture tex, int startX, int count, int y, int paletteIndex)
    {
        StaticBody2D body = new()
        {
            Position = new Vector2(startX + (count * 16) * 0.5f, y + 8)
        };
        CollisionShape2D shape = new()
        {
            Shape = new RectangleShape2D { Size = new Vector2(count * 16, 16) }
        };
        body.AddChild(shape);
        AddChild(body);

        Color tint = NesSprite.PresetPalettes[paletteIndex][1];
        for (int i = 0; i < count; i++)
        {
            Sprite2D tile = new()
            {
                Texture = tex,
                Centered = false,
                Position = new Vector2(startX + i * 16, y),
                Modulate = tint
            };
            AddChild(tile);
        }
    }

    private void SpawnPlayer()
    {
        _player = new SamplePlayer
        {
            GlobalPosition = new Vector2(64, 180)
        };
        AddChild(_player);
        _player.Sprite.ApplyPresetPalette(0);
    }

    private void SpawnScanlineTestBots()
    {
        // Spawn 9 bots along the ground floor scanline band (Y = 192)
        // Together with the player, that puts 10 sprites on the exact same 16 scanlines!
        for (int i = 0; i < 9; i++)
        {
            PatrolDummy bot = new()
            {
                GlobalPosition = new Vector2(40 + i * 34, 192),
                MinX = 28f,
                MaxX = 356f,
                Speed = 28f + (i * 5f),
                WaveAmplitude = (i % 3 == 0) ? 4f : 0f
            };
            AddChild(bot);
            bot.Sprite.ApplyPresetPalette((i % 4) + 1);
            _dummies.Add(bot);
        }

        // Spawn 3 bots on the upper platform (Y = 96)
        for (int i = 0; i < 3; i++)
        {
            PatrolDummy upperBot = new()
            {
                GlobalPosition = new Vector2(144 + i * 40, 96),
                MinX = 136f,
                MaxX = 248f,
                Speed = 35f + i * 8f,
                WaveAmplitude = 2f
            };
            AddChild(upperBot);
            upperBot.Sprite.ApplyPresetPalette(3);
            _dummies.Add(upperBot);
        }
    }

    private void BuildDebugHud()
    {
        _hudLabel = new Label
        {
            Position = new Vector2(6, 4),
            Modulate = new Color(0.95f, 0.95f, 0.95f, 1.0f)
        };
        _hudLabel.AddThemeFontSizeOverride("font_size", 8);
        AddChild(_hudLabel);
    }

    private void UpdateHudText()
    {
        DisplayManager dm = DisplayManager.Instance;
        NesSpriteManager sm = NesSpriteManager.Instance;

        _hudLabel.Text =
            $"RES: 384x216 | [F1] Scale: {dm.CurrentScale}x/{dm.MaxScale}x ({384 * dm.CurrentScale}x{216 * dm.CurrentScale}) | [F2] PPU Mode: {sm.CurrentMode}\n" +
            $"[P] Hero Palette: #{_player.Sprite.CurrentPaletteIndex} | [O] Bot Palettes | Peak Sprites/Scanline: {sm.MaxSpritesOnSingleScanlineLastFrame}/8 | Move: A/D + Space";
    }
}
