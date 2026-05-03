using System;

namespace HaengSungAOI_WPF.Machine.PLC.PLC
{
    /// <summary>
    /// Defines monitoring groups for PLC data points to enable dynamic activation/deactivation
    /// </summary>
    [Flags]
    public enum PLCMonitoringGroup
    {
        None = 0,
        
        /// <summary>
        /// MainWindow HMI control lamps (Auto, Manual, Reset, Start, Stop, etc.)
        /// Active when: MainWindow is active
        /// </summary>
        MainWindowHMILamps = 1 << 0,
        
        /// <summary>
        /// MainWindow HMI control buttons
        /// Active when: Always (for write operations)
        /// </summary>
        MainWindowHMIButtons = 1 << 1,
        
        /// <summary>
        /// Servo axis current positions (14 axes × 4 registers = 56 registers)
        /// Active when: ModelConfig or RobotJogWindow is active
        /// </summary>
        ServoPositions = 1 << 2,
        
        /// <summary>
        /// Servo axis error codes (14 axes × 4 registers = 56 registers)
        /// Active when: Always (for safety monitoring)
        /// </summary>
        ServoErrors = 1 << 3,
        
        /// <summary>
        /// Servo axis speeds, operation status (14 axes × 8 registers = 112 registers)
        /// Active when: ModelConfig or RobotJogWindow is active (optional)
        /// </summary>
        ServoStatus = 1 << 4,
        
        /// <summary>
        /// Servo axis target positions, speeds, points (14 axes × 10 registers = 140 registers)
        /// Active when: ModelConfig is active (for position saving)
        /// </summary>
        ServoTargets = 1 << 5,
        
        /// <summary>
        /// Servo HMI jog buttons (14 axes × 11 buttons = 154 registers)
        /// Active when: RobotJogWindow is active (for write operations)
        /// </summary>
        ServoJogButtons = 1 << 6,
        
        /// <summary>
        /// Servo HMI jog lamps (14 axes × 4 lamps = 56 registers)
        /// Active when: RobotJogWindow is active (for visual feedback)
        /// </summary>
        ServoJogLamps = 1 << 7,
        
        /// <summary>
        /// Servo HMI position buttons and lamps (14 axes × 22 = 308 registers)
        /// Active when: Rarely needed (disable by default)
        /// </summary>
        ServoPositionButtons = 1 << 8,
        
        /// <summary>
        /// Vision trigger and result tags
        /// Active when: Machine is running in auto mode
        /// </summary>
        VisionTriggers = 1 << 9,
        
        /// <summary>
        /// HMI select buttons and lamps
        /// Active when: Rarely needed
        /// </summary>
        HMISelect = 1 << 10,
        
        /// <summary>
        /// Machine alarms and errors from PLC (MW9000-MW9099)
        /// Active when: MainWindow is active (always monitored for safety)
        /// </summary>
        MachineAlarms = 1 << 11,
        
        /// <summary>
        /// All groups enabled (default for backward compatibility)
        /// </summary>
        All = ~0
    }

    /// <summary>
    /// PLC Constants - Centralized configuration for PLC monitoring task parameters
    /// Modify these values to adjust PLC communication behavior without changing code
    /// </summary>
    public static class PLCConstants
    {
        #region Connection Settings

        /// <summary>
        /// PLC IP Address for Modbus TCP connection
        /// </summary>
        public const string PLC_IP_ADDRESS = "192.168.100.50";

        /// <summary>
        /// Modbus TCP port (default: 502)
        /// </summary>
        public const int PLC_PORT = 502;

        /// <summary>
        /// Modbus unit identifier (slave ID)
        /// </summary>
        public const byte PLC_UNIT_IDENTIFIER = 1;

        #endregion

        #region Polling Intervals

        /// <summary>
        /// PLC read polling interval in milliseconds
        /// How often the PLC data points are read
        /// Recommended: 100-1000ms depending on response requirements
        /// Default: 1000ms (1 second)
        /// </summary>
        public const int PLC_READ_INTERVAL_MS = 100;

        /// <summary>
        /// Connection check interval in milliseconds
        /// How often the PLC connection status is verified
        /// Default: 5000ms (5 seconds)
        /// </summary>
        public const int PLC_CONNECTION_CHECK_INTERVAL_MS = 5000;

        /// <summary>
        /// Inter-command delay in milliseconds
        /// Minimum delay between consecutive write commands to prevent PLC poll cycle conflicts
        /// Recommended range: 20-50ms depending on PLC scan time
        /// Default: 20ms
        /// </summary>
        public const int PLC_INTER_COMMAND_DELAY_MS = 20;

        /// <summary>
        /// Write operation pause delay in milliseconds
        /// Delay to wait for any ongoing read to complete before writing
        /// Default: 50ms
        /// </summary>
        public const int PLC_WRITE_PAUSE_DELAY_MS = 50;

        #endregion

        #region Block Read Settings

        /// <summary>
        /// Number of coils to read per block
        /// Coils are packed 8 per byte, so 64 coils = 8 bytes
        /// Default: 64
        /// </summary>
        public const int COIL_BLOCK_SIZE = 64;

        /// <summary>
        /// Number of holding registers to read per block
        /// Each register is 16 bits (2 bytes)
        /// Default: 128 (increased for better throughput)
        /// </summary>
        public const int HOLDING_REGISTER_BLOCK_SIZE = 128;

        /// <summary>
        /// Number of discrete inputs to read per block
        /// Discrete inputs are packed 8 per byte, so 64 inputs = 8 bytes
        /// Default: 64
        /// </summary>
        public const int DISCRETE_INPUT_BLOCK_SIZE = 64;

        /// <summary>
        /// Number of input registers to read per block
        /// Each register is 16 bits (2 bytes)
        /// Default: 32
        /// </summary>
        public const int INPUT_REGISTER_BLOCK_SIZE = 32;

        #endregion

        #region Monitoring Group Settings

        /// <summary>
        /// Default monitoring groups enabled on startup
        /// MainWindow lamps + Servo errors are always monitored for safety
        /// </summary>
        public const PLCMonitoringGroup DEFAULT_MONITORING_GROUPS = 
            PLCMonitoringGroup.MainWindowHMILamps | 
            PLCMonitoringGroup.MainWindowHMIButtons |
            PLCMonitoringGroup.ServoErrors |
            PLCMonitoringGroup.MachineAlarms;

        /// <summary>
        /// Monitoring groups enabled when ModelConfig window is active
        /// Adds servo positions and targets for position management
        /// </summary>
        public const PLCMonitoringGroup MODELCONFIG_MONITORING_GROUPS = 
            PLCMonitoringGroup.MainWindowHMILamps |
            PLCMonitoringGroup.MainWindowHMIButtons |
            PLCMonitoringGroup.ServoErrors |
            PLCMonitoringGroup.ServoPositions |
            PLCMonitoringGroup.ServoTargets |
            PLCMonitoringGroup.MachineAlarms;

        /// <summary>
        /// Monitoring groups enabled when RobotJogWindow is active
        /// Adds jog buttons/lamps and positions for jog control
        /// </summary>
        public const PLCMonitoringGroup ROBOTJOG_MONITORING_GROUPS = 
            PLCMonitoringGroup.ServoErrors |
            PLCMonitoringGroup.ServoPositions |
            PLCMonitoringGroup.ServoJogButtons |
            PLCMonitoringGroup.ServoJogLamps |
            PLCMonitoringGroup.MachineAlarms;

        /// <summary>
        /// Monitoring groups enabled during auto mode operation
        /// Includes vision triggers and essential servo monitoring
        /// </summary>
        public const PLCMonitoringGroup AUTOMODE_MONITORING_GROUPS = 
            PLCMonitoringGroup.MainWindowHMILamps |
            PLCMonitoringGroup.MainWindowHMIButtons |
            PLCMonitoringGroup.ServoErrors |
            PLCMonitoringGroup.ServoPositions |
            PLCMonitoringGroup.VisionTriggers |
            PLCMonitoringGroup.MachineAlarms;

        #endregion

        #region Vision Trigger Tags

        /// <summary>
        /// Vision trigger tag start address (MW400)
        /// </summary>
        public const ushort VISION_TRIGGER_START_ADDRESS = 400;

        /// <summary>
        /// Vision result tag start address (MW410)
        /// </summary>
        public const ushort VISION_RESULT_START_ADDRESS = 410;

        /// <summary>
        /// Align X position tag address (MW420) - LREAL (4 registers)
        /// </summary>
        public const ushort ALIGN_X_ADDRESS = 420;

        /// <summary>
        /// Align Y position tag address (MW424) - LREAL (4 registers)
        /// </summary>
        public const ushort ALIGN_Y_ADDRESS = 424;

        /// <summary>
        /// Align R/Angle position tag address (MW428) - LREAL (4 registers)
        /// </summary>
        public const ushort ALIGN_R_ADDRESS = 428;

        /// <summary>
        /// Align X position tag name
        /// </summary>
        public const string ALIGN_X_TAG = "MW420";

        /// <summary>
        /// Align Y position tag name
        /// </summary>
        public const string ALIGN_Y_TAG = "MW424";

        /// <summary>
        /// Align R/Angle position tag name
        /// </summary>
        public const string ALIGN_R_TAG = "MW428";

        #endregion

        #region Timeout Settings

        /// <summary>
        /// PLC connection timeout in milliseconds
        /// Default: 5000ms (5 seconds)
        /// </summary>
        public const int PLC_CONNECTION_TIMEOUT_MS = 5000;

        /// <summary>
        /// PLC read timeout in milliseconds
        /// Default: 1000ms (1 second)
        /// </summary>
        public const int PLC_READ_TIMEOUT_MS = 1000;

        /// <summary>
        /// PLC write timeout in milliseconds
        /// Default: 1000ms (1 second)
        /// </summary>
        public const int PLC_WRITE_TIMEOUT_MS = 1000;

        #endregion

        #region Error Handling

        /// <summary>
        /// Maximum number of consecutive read failures before marking connection as lost
        /// Default: 3
        /// </summary>
        public const int MAX_CONSECUTIVE_READ_FAILURES = 3;

        /// <summary>
        /// Maximum number of consecutive write failures before raising error
        /// Default: 3
        /// </summary>
        public const int MAX_CONSECUTIVE_WRITE_FAILURES = 3;

        /// <summary>
        /// Reconnection delay after connection loss in milliseconds
        /// Default: 2000ms (2 seconds)
        /// </summary>
        public const int PLC_RECONNECTION_DELAY_MS = 2000;

        #endregion
    }
}
