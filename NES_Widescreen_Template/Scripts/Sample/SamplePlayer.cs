using Godot;
using NesTemplate.Core;

namespace NesTemplate.Sample;

public partial class SamplePlayer : CharacterBody2D
{
    public const float MoveSpeed = 110.0f;
    public const float GroundAccel = 750.0f;
    public const float GroundFriction = 850.0f;
    public const float Gravity = 620.0f;
    public const float JumpVelocity = -215.0f;

    public NesSprite Sprite { get; private set; } = null!;

    public override void _Ready()
    {
        CollisionShape2D collision = new()
        {
            Shape = new RectangleShape2D { Size = new Vector2(12, 16) }
        };
        AddChild(collision);

        Sprite = new NesSprite
        {
            Texture = PixelArtGenerator.CreateHeroTexture(),
            Centered = true
        };
        AddChild(Sprite);
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;
        Vector2 velocity = Velocity;

        // Gravity
        if (!IsOnFloor())
        {
            velocity.Y = Mathf.Min(velocity.Y + Gravity * dt, 320.0f);
        }

        // Jump (Space, Z, Up, or W)
        bool jumpJustPressed = Input.IsKeyPressed(Key.Space) || Input.IsKeyPressed(Key.Z) || Input.IsKeyPressed(Key.W) || Input.IsKeyPressed(Key.Up);
        if (jumpJustPressed && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }
        else if (!jumpJustPressed && velocity.Y < -70.0f)
        {
            // Variable jump release cut
            velocity.Y = -70.0f;
        }

        // Horizontal Movement
        float moveDir = 0f;
        if (Input.IsKeyPressed(Key.Left) || Input.IsKeyPressed(Key.A))
            moveDir -= 1f;
        if (Input.IsKeyPressed(Key.Right) || Input.IsKeyPressed(Key.D))
            moveDir += 1f;

        if (Mathf.Abs(moveDir) > 0.01f)
        {
            velocity.X = Mathf.MoveToward(velocity.X, moveDir * MoveSpeed, GroundAccel * dt);
            Sprite.FlipH = moveDir < 0f;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0f, GroundFriction * dt);
        }

        Velocity = velocity;
        MoveAndSlide();

        // Keep within the 384x216 screen bounds
        GlobalPosition = new Vector2(
            Mathf.Clamp(GlobalPosition.X, 16f, 368f),
            Mathf.Clamp(GlobalPosition.Y, 16f, 200f)
        );
    }
}
