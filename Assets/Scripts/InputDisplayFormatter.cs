public static class InputDisplayFormatter
{
    //TODO: CHANGE THIS INTO IMAGE INSTEAD

    public static string GetPrettyName(string rawBinding)
    {
        if (string.IsNullOrEmpty(rawBinding) || rawBinding == "None")
            return "Not Bound";

        // 1. Handle Composite Split Axes (e.g., "A/S" or "LeftArrow/RightArrow")
        if (rawBinding.Contains("/"))
        {
            string[] split = rawBinding.Split('/');
            if (split.Length == 2)
            {
                return $"{FormatSingleKey(split[0])} / {FormatSingleKey(split[1])}";
            }
        }

        // 2. Handle standard inputs
        return FormatSingleKey(rawBinding);
    }

    private static string FormatSingleKey(string keyString)
    {
        // Custom mapping dictionary for cleaner visuals
        switch (keyString)
        {
            // Mouse
            case "Mouse0": return "Left Click";
            case "Mouse1": return "Right Click";
            case "Mouse2": return "Middle Click";

            // Keyboard Cleanups
            case "Alpha0": return "0";
            case "Alpha1": return "1";
            case "Alpha2": return "2";
            case "Alpha3": return "3";
            case "Alpha4": return "4";
            case "Alpha5": return "5";
            case "Alpha6": return "6";
            case "Alpha7": return "7";
            case "Alpha8": return "8";
            case "Alpha9": return "9";
            case "Return": return "Enter";
            case "Escape": return "Esc";
            case "Space": return "Spacebar";
            case "LeftShift": return "L-Shift";
            case "RightShift": return "R-Shift";
            case "LeftControl": return "L-Ctrl";
            case "RightControl": return "R-Ctrl";

            // Controller Continuous Axes
            case "LeftStickX": return "Left Stick ↔";
            case "LeftStickY": return "Left Stick ↕";
            case "JoystickAxis9": return "Left Trigger";
            case "JoystickAxis10": return "Right Trigger";

            // Controller Standard Buttons (Xbox Global Layout defaults)
            case "JoystickButton0": return "Button South (A / ×)";
            case "JoystickButton1": return "Button East (B / ○)";
            case "JoystickButton2": return "Button West (X / ▢)";
            case "JoystickButton3": return "Button North (Y / △)";
            case "JoystickButton4": return "Left Bumper (LB)";
            case "JoystickButton5": return "Right Bumper (RB)";
            case "JoystickButton6": return "View / Share Button";
            case "JoystickButton7": return "Menu / Options Button";
            case "JoystickButton8": return "Left Stick Click";
            case "JoystickButton9": return "Right Stick Click";

            default:
                // If it's a standard letter (like "W", "A", "S", "D"), return it raw
                return keyString;
        }
    }
}