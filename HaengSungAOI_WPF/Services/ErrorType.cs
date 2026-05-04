namespace HaengSungAOI_WPF.Services
{
    /// <summary>
    /// Type of error that occurred in the machine system
    /// </summary>
    public enum ErrorType
    {
        Information,     // Informational message, not an actual error
        Warning,         // Warning that doesn't stop operation but needs attention
        Error,           // Error that affects operation but isn't critical
        Critical,        // Critical error that requires immediate attention
        Collision,       // Collision detection or prevention triggered
        Timeout,         // Operation timeout
        Hardware,        // Hardware-related error (axis, IO, etc.)
        Vision,          // Vision system error
        Communication,   // Communication error with external systems
        PLC,             // PLC system error
        Robot,           // Robot-specific error
        Safety,          // Safety system error
        System           // General system error
    }
}
