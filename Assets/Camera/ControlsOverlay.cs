using UnityEngine;

public static class ControlsOverlay
{
    public const string ToggleKeyLabel = "\u00B2";

    private const char ToggleCharacter = '\u00B2';

    private static bool _visible = true;
    private static int _toggledFrame = -1;

    public static bool Visible
    {
        get { return _visible; }
    }

    public static void HandleToggle()
    {
        if (_toggledFrame == Time.frameCount)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.BackQuote) || Input.inputString.IndexOf(ToggleCharacter) >= 0)
        {
            _visible = !_visible;
            _toggledFrame = Time.frameCount;
        }
    }
}
