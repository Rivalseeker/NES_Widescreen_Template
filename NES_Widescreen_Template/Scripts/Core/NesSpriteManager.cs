using Godot;
using System;
using System.Collections.Generic;

namespace NesTemplate.Core;

public enum NesLimitMode
{
    OffUnlimited = 0,
    StrictOverflowClip = 1,
    AuthenticOamFlicker = 2
}

public partial class NesSpriteManager : Node
{
    public static NesSpriteManager Instance { get; private set; } = null!;

    public const int ViewportHeight = 216;
    public const int MaxSpritesPerScanline = 8;

    public NesLimitMode CurrentMode { get; set; } = NesLimitMode.AuthenticOamFlicker;
    public int MaxSpritesOnSingleScanlineLastFrame { get; private set; }

    private readonly List<NesSprite> _registeredSprites = new();
    private readonly int[] _scanlineCounts = new int[ViewportHeight];
    private int _oamRotationOffset = 0;

    // Prime stride used by classic NES OAM shufflers so adjacent sprites cycle smoothly
    private const int PrimeStride = 5;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public void RegisterSprite(NesSprite sprite)
    {
        if (!_registeredSprites.Contains(sprite))
            _registeredSprites.Add(sprite);
    }

    public void UnregisterSprite(NesSprite sprite)
    {
        _registeredSprites.Remove(sprite);
    }

    public void CycleLimitMode()
    {
        int next = ((int)CurrentMode + 1) % 3;
        CurrentMode = (NesLimitMode)next;
    }

    public override void _Process(double delta)
    {
        int spriteCount = _registeredSprites.Count;
        if (spriteCount == 0)
            return;

        if (CurrentMode == NesLimitMode.OffUnlimited)
        {
            MaxSpritesOnSingleScanlineLastFrame = 0;
            for (int i = 0; i < spriteCount; i++)
            {
                _registeredSprites[i].SetScanlineLimitEnabled(false);
            }
            return;
        }

        Array.Clear(_scanlineCounts, 0, ViewportHeight);

        // Rotate starting OAM slot each frame when in AuthenticOamFlicker mode
        if (CurrentMode == NesLimitMode.AuthenticOamFlicker)
        {
            _oamRotationOffset = (_oamRotationOffset + PrimeStride) % Math.Max(1, spriteCount);
        }
        else
        {
            _oamRotationOffset = 0;
        }

        // Convert world coordinates to screen scanlines using current canvas transform
        Transform2D canvasTransform = GetViewport().CanvasTransform;

        for (int step = 0; step < spriteCount; step++)
        {
            // Determine which sprite is evaluated in this OAM slot
            int index = (CurrentMode == NesLimitMode.AuthenticOamFlicker)
                ? (_oamRotationOffset + step * PrimeStride) % spriteCount
                : step;

            // Ensure coprime traversal fallback if spriteCount is divisible by PrimeStride
            if (CurrentMode == NesLimitMode.AuthenticOamFlicker && (spriteCount % PrimeStride == 0))
            {
                index = (_oamRotationOffset + step) % spriteCount;
            }

            NesSprite sprite = _registeredSprites[index];
            if (!sprite.IsVisibleInTree())
                continue;

            sprite.SetScanlineLimitEnabled(true);
            Rect2 screenRect = sprite.GetScreenBoundingRect(canvasTransform);

            int startY = Mathf.Clamp(Mathf.FloorToInt(screenRect.Position.Y), 0, ViewportHeight - 1);
            int endY = Mathf.Clamp(Mathf.CeilToInt(screenRect.End.Y) - 1, 0, ViewportHeight - 1);

            int[] mask = sprite.ScanlineMaskBuffer;
            // Default all 256 bits to 1 (visible) outside the sprite's vertical bounds
            for (int b = 0; b < 8; b++)
                mask[b] = -1; // 0xFFFFFFFF in two's complement

            // Evaluate each scanline this sprite covers
            for (int y = startY; y <= endY; y++)
            {
                _scanlineCounts[y]++;
                if (_scanlineCounts[y] > MaxSpritesPerScanline)
                {
                    // Overflow! Clear this scanline's visibility bit for this sprite
                    int bucket = y >> 5;
                    int bit = y & 31;
                    mask[bucket] &= ~(1 << bit);
                }
            }

            sprite.PushScanlineMaskToShader();
        }

        int peak = 0;
        for (int y = 0; y < ViewportHeight; y++)
        {
            if (_scanlineCounts[y] > peak)
                peak = _scanlineCounts[y];
        }
        MaxSpritesOnSingleScanlineLastFrame = peak;
    }
}
