using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Machine
{
    public enum RobotControlMode
    {
        Manual,
        Auto
    }

    public enum RobotState
    {
        Idle,
        MovingToPickup,
        AtPickup,
        VacuumOn,
        WaitingForPCB,
        MovingToPlace,
        AtPlace,
        PlacingPCB,
        MovingToIdle,
        Error,
        Busy
    }

    public enum SequenceStep
    {
        Idle,
        GoToPickup,
        TurnOnVacuum,
        WaitForVacuumSensor,
        MoveToIdleFromPickup,
        MoveToPreparePlace,
        MoveToPlace,
        ReleasePCB,
        MoveToPreparePlaceFromPlace,
        MoveToIdleFromPlace,
        Completed
    }

    public enum MachineSequenceState
    {
        Idle,
        Running
    }

    internal class Enums
    {
        public enum EnumType
        {
        }
    }
}
