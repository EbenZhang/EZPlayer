using System.Windows.Input;

namespace EZPlayer
{
    public static class ShortKeys
    {
        public static bool IsPauseShortKey(KeyEventArgs e)
        {
            return e.Key == Key.Space;
        }

        public static bool IsIncreaseVolumeShortKey(KeyEventArgs e)
        {
            return e.Key == Key.Up
                && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }

        public static bool IsDecreaseVolumeShortKey(KeyEventArgs e)
        {
            return e.Key == Key.Down
                && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }

        public static bool IsForwardShortKey(KeyEventArgs e)
        {
            return e.Key == Key.Right
                && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }

        public static bool IsRewindShortKey(KeyEventArgs e)
        {
            return e.Key == Key.Left
                && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        }
        public static bool IsFullScreenShortKey(KeyEventArgs e)
        {
            bool controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            return e.Key == Key.Enter && controlPressed;
        }
    }
}
