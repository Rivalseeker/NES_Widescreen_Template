using Godot;
using NesTemplate.Core;

namespace NesTemplate.Sample;

public partial class PatrolDummy : Node2D
{
    public float MinX { get; set; } = 32f;
    public float MaxX { get; set; } = 352f;
    public float Speed { get; set; } = 45f;
    public float WaveAmplitude { get; set; } = 0f;
    public float WaveFrequency { get; set; } = 3f;

    public NesSprite Sprite { get; private set; } = null!;

    private float _baseY;
    private float _direction = 1f;
    private float _timeOffset;

    public override void _Ready()
    {
        _baseY = GlobalPosition.Y;
        _timeOffset = (float)GD.RandRange(0.0, 6.28);

        Sprite = new NesSprite
        {
            Texture = PixelArtGenerator.CreateBotTexture(),
            Centered = true
        };
        AddChild(Sprite);
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _timeOffset += dt * WaveFrequency;

        float nextX = GlobalPosition.X + Speed * _direction * dt;
        if (nextX >= MaxX)
        {
            nextX = MaxX;
            _direction = -1f;
            Sprite.FlipH = true;
        }
        else if (nextX <= MinX)
        {
            nextX = MinX;
            _direction = 1f;
            Sprite.FlipH = false;
        }

        float offsetY = WaveAmplitude > 0.01f ? Mathf.Sin(_timeOffset) * WaveAmplitude : 0f;
        GlobalPosition = new Vector2(nextX, Mathf.Round(_baseY + offsetY));
    }
}
