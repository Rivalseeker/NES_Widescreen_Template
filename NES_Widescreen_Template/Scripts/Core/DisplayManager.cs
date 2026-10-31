using Godot;
using System;

namespace NesTemplate.Core;

public partial class DisplayManager : Node
{
    public static DisplayManager Instance { get; private set; } = null!;

    public const int BaseWidth = 384;
    public const int BaseHeight = 216;

    public int CurrentScale { get; private set; } = 1;
    public int MaxScale { get; private set; } = 1;

    private Vector2I _pendingWindowSize;
    private int _pendingScreen;
    private Vector2I _pendingScreenSize;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        Window rootWindow = GetTree().Root;
        rootWindow.Unresizable = true;
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.ResizeDisabled, true);

        RecalculateMaxScale();

        // Default to the largest comfortable integer scale that leaves room for window borders,
        // or MaxScale if MaxScale is 1 or 2.
        int initialScale = Math.Max(1, MaxScale > 2 ? MaxScale - 1 : MaxScale);
        SetWindowScale(initialScale);
    }

    public void RecalculateMaxScale()
    {
        int currentScreen = DisplayServer.WindowGetCurrentScreen();
        Vector2I screenSize = DisplayServer.ScreenGetSize(currentScreen);

        int maxScaleX = screenSize.X / BaseWidth;
        int maxScaleY = screenSize.Y / BaseHeight;

        MaxScale = Math.Max(1, Math.Min(maxScaleX, maxScaleY));
    }

    public void SetWindowScale(int targetScale)
    {
        RecalculateMaxScale();
        CurrentScale = Mathf.Clamp(targetScale, 1, MaxScale);

        Vector2I newWindowSize = new Vector2I(BaseWidth * CurrentScale, BaseHeight * CurrentScale);

        int currentScreen = DisplayServer.WindowGetCurrentScreen();
        Vector2I screenSize = DisplayServer.ScreenGetSize(currentScreen);
        Rect2I usableRect = DisplayServer.ScreenGetUsableRect(currentScreen);

        // Enter fullscreen if we are at MaxScale and the window reaches or exceeds the monitor's usable area
        bool useFullscreen = (CurrentScale == MaxScale) &&
                             (newWindowSize.X >= usableRect.Size.X || newWindowSize.Y >= usableRect.Size.Y);

        Window rootWindow = GetTree().Root;

        if (useFullscreen)
        {
            rootWindow.Mode = Window.ModeEnum.Fullscreen;
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        }
        else
        {
            // Explicitly exit Fullscreen/Maximized mode first; otherwise Godot's DisplayServer
            // ignores WindowSetSize and Borderless flag changes while in Fullscreen mode.
            if (DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed ||
                rootWindow.Mode != Window.ModeEnum.Windowed)
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                rootWindow.Mode = Window.ModeEnum.Windowed;
            }

            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
            rootWindow.Borderless = false;

            DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.ResizeDisabled, true);
            rootWindow.Unresizable = true;

            DisplayServer.WindowSetSize(newWindowSize);
            rootWindow.Size = newWindowSize;
            CenterWindowOnScreen(currentScreen, screenSize, newWindowSize);

            // Re-apply on the next frame in case the OS window manager asynchronously resets
            // the window rect when transitioning out of fullscreen/borderless mode.
            _pendingWindowSize = newWindowSize;
            _pendingScreen = currentScreen;
            _pendingScreenSize = screenSize;
            CallDeferred(MethodName.ApplyDeferredWindowGeometry);
        }
    }

    private void ApplyDeferredWindowGeometry()
    {
        Window rootWindow = GetTree().Root;
        if (rootWindow.Mode != Window.ModeEnum.Windowed)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            rootWindow.Mode = Window.ModeEnum.Windowed;
        }

        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
        rootWindow.Borderless = false;

        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.ResizeDisabled, true);
        rootWindow.Unresizable = true;

        DisplayServer.WindowSetSize(_pendingWindowSize);
        rootWindow.Size = _pendingWindowSize;

        CenterWindowOnScreen(_pendingScreen, _pendingScreenSize, _pendingWindowSize);
    }

    public void CycleWindowScale(int direction = 1)
    {
        RecalculateMaxScale();
        int nextScale = CurrentScale + direction;
        if (nextScale > MaxScale)
            nextScale = 1;
        else if (nextScale < 1)
            nextScale = MaxScale;

        SetWindowScale(nextScale);
    }

    private static void CenterWindowOnScreen(int screenIndex, Vector2I screenSize, Vector2I windowSize)
    {
        Vector2I screenPos = DisplayServer.ScreenGetPosition(screenIndex);
        Vector2I centeredPos = screenPos + (screenSize - windowSize) / 2;
        DisplayServer.WindowSetPosition(centeredPos);
    }
}