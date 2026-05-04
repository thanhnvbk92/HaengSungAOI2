using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Models
{
    /// <summary>
    /// Data model for robot position entries in simplified table format
    /// </summary>
    public class RobotPositionEntry : INotifyPropertyChanged
    {
        private string _position;
        private float _x;
        private float _y;
        private float _r;
        private float _z;
        private float _c;
        private float _speedX;
        private float _speedY;
        private float _speedR;
        private float _speedZ;
        private float _speed;
        private float _accel;
        private float _decel;

        public string Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged(nameof(Position));
            }
        }

        public float X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }

        public float Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }

        public float R
        {
            get => _r;
            set
            {
                _r = value;
                OnPropertyChanged(nameof(R));
            }
        }

        public float Z
        {
            get => _z;
            set
            {
                _z = value;
                OnPropertyChanged(nameof(Z));
            }
        }

        public float C
        {
            get => _c;
            set
            {
                _c = value;
                OnPropertyChanged(nameof(C));
            }
        }

        public float SpeedX
        {
            get => _speedX;
            set
            {
                _speedX = value;
                OnPropertyChanged(nameof(SpeedX));
            }
        }

        public float SpeedY
        {
            get => _speedY;
            set
            {
                _speedY = value;
                OnPropertyChanged(nameof(SpeedY));
            }
        }

        public float SpeedR
        {
            get => _speedR;
            set
            {
                _speedR = value;
                OnPropertyChanged(nameof(SpeedR));
            }
        }

        public float SpeedZ
        {
            get => _speedZ;
            set
            {
                _speedZ = value;
                OnPropertyChanged(nameof(SpeedZ));
            }
        }

        public float Speed
        {
            get => _speed;
            set
            {
                _speed = value;
                OnPropertyChanged(nameof(Speed));
            }
        }

        public float Accel
        {
            get => _accel;
            set
            {
                _accel = value;
                OnPropertyChanged(nameof(Accel));
            }
        }

        public float Decel
        {
            get => _decel;
            set
            {
                _decel = value;
                OnPropertyChanged(nameof(Decel));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Enum for robot types used in jog window and model config
    /// </summary>
    public enum RobotType
    {
        Infeed,
        Transfer,
        Outfeed,
        Inspect1,
        Inspect2
    }

    public class PCBModel
    {
        // Database metadata
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ModelName 
        { 
            get => Name; 
            set => Name = value; 
        }
        public string Description { get; set; } = "";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = false;

        // Vision Solution Configuration
        public string VisionSolutionPath { get; set; } = "";
        public string VisionSolutionName { get; set; } = "";

        // Infeed Robot Z positions
        public float PCBInfeedPick_Z;
        public float PCBInfeedPlace_Z;

        // Infeed Robot speeds and timing (legacy - kept for backward compatibility)
        public float PCBInfeedPick_Speed;
        public float PCBInfeedPlace_Speed;
        public float PCBInfeedPick_Acceleration;
        public float PCBInfeedPlace_Acceleration;
        public float PCBInfeedPick_Deceleration;
        public float PCBInfeedPlace_Deceleration;

        // Infeed Robot positions (3 defined positions: idle, pickup, place)
        public float PCBInfeed_IdleX;
        public float PCBInfeed_IdleY;
        public float PCBInfeed_IdleZ;
        public float PCBInfeed_IdleR;

        public float PCBInfeed_PickupX;
        public float PCBInfeed_PickupY;
        public float PCBInfeed_PickupZ;
        public float PCBInfeed_PickupR;

        public float PCBInfeed_PreparePlaceX;
        public float PCBInfeed_PreparePlaceY;
        public float PCBInfeed_PreparePlaceZ;
        public float PCBInfeed_PreparePlaceR;

        public float PCBInfeed_PlaceX;
        public float PCBInfeed_PlaceY;
        public float PCBInfeed_PlaceZ;
        public float PCBInfeed_PlaceR;

        // Infeed Robot individual speeds per step per axis
        // Idle position speeds
        public float PCBInfeed_Idle_SpeedX;
        public float PCBInfeed_Idle_SpeedY;
        public float PCBInfeed_Idle_SpeedR;
        public float PCBInfeed_Idle_Accel;
        public float PCBInfeed_Idle_Decel;
        // Pickup position speeds
        public float PCBInfeed_Pickup_SpeedX;
        public float PCBInfeed_Pickup_SpeedY;
        public float PCBInfeed_Pickup_SpeedR;
        public float PCBInfeed_Pickup_Accel;
        public float PCBInfeed_Pickup_Decel;
        // Place position speeds
        public float PCBInfeed_Place_SpeedX;
        public float PCBInfeed_Place_SpeedY;
        public float PCBInfeed_Place_SpeedR;
        public float PCBInfeed_Place_Accel;
        public float PCBInfeed_Place_Decel;

        // Transfer Robot positions (5 defined positions: Idle, Prepare pickup, Pickup, Prepare place, Place)
        public float PCBTransfer_IdleX;
        public float PCBTransfer_IdleZ;

        public float PCBTransfer_PreparePickupX;
        public float PCBTransfer_PreparePickupZ;

        public float PCBTransfer_PickupX;
        public float PCBTransfer_PickupZ;

        public float PCBTransfer_PreparePlaceX;
        public float PCBTransfer_PreparePlaceZ;

        public float PCBTransfer_PlaceX;
        public float PCBTransfer_PlaceZ;

        // Transfer Robot NG position
        public float PCBTransfer_NGX;
        public float PCBTransfer_NGZ;

        // Transfer Robot speeds and timing (legacy - kept for backward compatibility)
        public float PCBTransfer_Speed;
        public float PCBTransfer_Acceleration;
        public float PCBTransfer_Deceleration;

        // Transfer Robot individual speeds per step per axis
        // Idle position speeds
        public float PCBTransfer_Idle_SpeedX;
        public float PCBTransfer_Idle_SpeedZ;
        public float PCBTransfer_Idle_Accel;
        public float PCBTransfer_Idle_Decel;
        // Prepare Pickup position speeds
        public float PCBTransfer_PreparePickup_SpeedX;
        public float PCBTransfer_PreparePickup_SpeedZ;
        public float PCBTransfer_PreparePickup_Accel;
        public float PCBTransfer_PreparePickup_Decel;
        // Pickup position speeds
        public float PCBTransfer_Pickup_SpeedX;
        public float PCBTransfer_Pickup_SpeedZ;
        public float PCBTransfer_Pickup_Accel;
        public float PCBTransfer_Pickup_Decel;
        // Prepare Place position speeds
        public float PCBTransfer_PreparePlace_SpeedX;
        public float PCBTransfer_PreparePlace_SpeedZ;
        public float PCBTransfer_PreparePlace_Accel;
        public float PCBTransfer_PreparePlace_Decel;
        // Place position speeds
        public float PCBTransfer_Place_SpeedX;
        public float PCBTransfer_Place_SpeedZ;
        public float PCBTransfer_Place_Accel;
        public float PCBTransfer_Place_Decel;
        // NG position speeds
        public float PCBTransfer_NG_SpeedX;
        public float PCBTransfer_NG_SpeedZ;
        public float PCBTransfer_NG_Accel;
        public float PCBTransfer_NG_Decel;

        // Outfeed Robot positions (6 defined positions: Idle, Pickup, OK, NG, Pickup tray, Place tray)
        public float PCBOutfeed_IdleX;
        public float PCBOutfeed_IdleY;
        public float PCBOutfeed_IdleZ;

        public float PCBOutfeed_PickupX;
        public float PCBOutfeed_PickupY;
        public float PCBOutfeed_PickupZ;

        // Outfeed Robot OK Place positions (for passed inspection)
        public float PCBOutfeed_PlaceOK1X;
        public float PCBOutfeed_PlaceOK1Y;
        public float PCBOutfeed_PlaceOK1Z;
        public float PCBOutfeed_PlaceOK2X;
        public float PCBOutfeed_PlaceOK2Y;
        public float PCBOutfeed_PlaceOK2Z;
        public float PCBOutfeed_PlaceOK3X;
        public float PCBOutfeed_PlaceOK3Y;
        public float PCBOutfeed_PlaceOK3Z;
        public float PCBOutfeed_PlaceOK4X;
        public float PCBOutfeed_PlaceOK4Y;
        public float PCBOutfeed_PlaceOK4Z;
        public float PCBOutfeed_PlaceOK5X;
        public float PCBOutfeed_PlaceOK5Y;
        public float PCBOutfeed_PlaceOK5Z;
        public float PCBOutfeed_PlaceOK6X;
        public float PCBOutfeed_PlaceOK6Y;
        public float PCBOutfeed_PlaceOK6Z;

        // Outfeed Robot NG Place positions (for failed inspection)
        public float PCBOutfeed_PlaceNGX;
        public float PCBOutfeed_PlaceNGY;
        public float PCBOutfeed_PlaceNGZ;

        // Outfeed Robot Pickup Tray positions
        public float PCBOutfeed_PickupTrayX;
        public float PCBOutfeed_PickupTrayY;
        public float PCBOutfeed_PickupTrayZ;

        // Outfeed Robot Place Tray positions
        public float PCBOutfeed_PlaceTrayX;
        public float PCBOutfeed_PlaceTrayY;
        public float PCBOutfeed_PlaceTrayZ;

        // Legacy single place position (for backward compatibility)
        public float PCBOutfeed_PlaceX;
        public float PCBOutfeed_PlaceY;
        public float PCBOutfeed_PlaceZ;

        // Outfeed Robot speeds and timing (legacy - kept for backward compatibility)
        public float PCBOutfeed_Speed;
        public float PCBOutfeed_Acceleration;
        public float PCBOutfeed_Deceleration;

        // Outfeed Robot individual speeds per step per axis
        // Idle position speeds
        public float PCBOutfeed_Idle_SpeedX;
        public float PCBOutfeed_Idle_SpeedY;
        public float PCBOutfeed_Idle_Accel;
        public float PCBOutfeed_Idle_Decel;
        // Pickup position speeds
        public float PCBOutfeed_Pickup_SpeedX;
        public float PCBOutfeed_Pickup_SpeedY;
        public float PCBOutfeed_Pickup_Accel;
        public float PCBOutfeed_Pickup_Decel;
        // OK Place 1 position speeds
        public float PCBOutfeed_PlaceOK1_SpeedX;
        public float PCBOutfeed_PlaceOK1_SpeedY;
        public float PCBOutfeed_PlaceOK1_Accel;
        public float PCBOutfeed_PlaceOK1_Decel;
        // OK Place 2 position speeds
        public float PCBOutfeed_PlaceOK2_SpeedX;
        public float PCBOutfeed_PlaceOK2_SpeedY;
        public float PCBOutfeed_PlaceOK2_Accel;
        public float PCBOutfeed_PlaceOK2_Decel;
        // OK Place 3 position speeds
        public float PCBOutfeed_PlaceOK3_SpeedX;
        public float PCBOutfeed_PlaceOK3_SpeedY;
        public float PCBOutfeed_PlaceOK3_Accel;
        public float PCBOutfeed_PlaceOK3_Decel;
        // OK Place 4 position speeds
        public float PCBOutfeed_PlaceOK4_SpeedX;
        public float PCBOutfeed_PlaceOK4_SpeedY;
        public float PCBOutfeed_PlaceOK4_Accel;
        public float PCBOutfeed_PlaceOK4_Decel;
        // OK Place 5 position speeds
        public float PCBOutfeed_PlaceOK5_SpeedX;
        public float PCBOutfeed_PlaceOK5_SpeedY;
        public float PCBOutfeed_PlaceOK5_Accel;
        public float PCBOutfeed_PlaceOK5_Decel;
        // OK Place 6 position speeds
        public float PCBOutfeed_PlaceOK6_SpeedX;
        public float PCBOutfeed_PlaceOK6_SpeedY;
        public float PCBOutfeed_PlaceOK6_Accel;
        public float PCBOutfeed_PlaceOK6_Decel;
        // NG Place position speeds
        public float PCBOutfeed_PlaceNG_SpeedX;
        public float PCBOutfeed_PlaceNG_SpeedY;
        public float PCBOutfeed_PlaceNG_Accel;
        public float PCBOutfeed_PlaceNG_Decel;
        // Pickup Tray position speeds
        public float PCBOutfeed_PickupTray_SpeedX;
        public float PCBOutfeed_PickupTray_SpeedY;
        public float PCBOutfeed_PickupTray_Accel;
        public float PCBOutfeed_PickupTray_Decel;
        // Place Tray position speeds
        public float PCBOutfeed_PlaceTray_SpeedX;
        public float PCBOutfeed_PlaceTray_SpeedY;
        public float PCBOutfeed_PlaceTray_Accel;
        public float PCBOutfeed_PlaceTray_Decel;

        //Independant axes positions
        public float Camera2_Focus_1;
        public float Camera2_Focus_2;
        public float Camera2_Focus_3;
        public float Camera3_Focus_1;
        public float Camera3_Focus_2;
        public float Camera3_Focus_3;

        public float PCBRotate1_Rotate1;
        public float PCBRotate1_Rotate2;
        public float PCBRotate1_Rotate3;
        public float PCBRotate2_Rotate1;
        public float PCBRotate2_Rotate2;
        public float PCBRotate2_Rotate3;

        // Inspect 1 Robot parameters (Z3 Camera Focus, C2 PCB Rotate)
        public float Inspect1_Focus1;
        public float Inspect1_Focus2;
        public float Inspect1_Focus3;
        public float Inspect1_Rotate1;
        public float Inspect1_Rotate2;
        public float Inspect1_Rotate3;
        public float Inspect1_IdleZ;
        public float Inspect1_IdleR;
        public float Inspect1_Speed;
        public float Inspect1_AccTime;
        public float Inspect1_DecTime;

        // Inspect 1 Robot individual speeds per step per axis
        // Idle position speeds
        public float Inspect1_Idle_SpeedZ;
        public float Inspect1_Idle_SpeedC;
        public float Inspect1_Idle_Accel;
        public float Inspect1_Idle_Decel;
        // Focus 1 position speeds
        public float Inspect1_Focus1_SpeedZ;
        public float Inspect1_Focus1_SpeedC;
        public float Inspect1_Focus1_Accel;
        public float Inspect1_Focus1_Decel;
        // Focus 2 position speeds
        public float Inspect1_Focus2_SpeedZ;
        public float Inspect1_Focus2_SpeedC;
        public float Inspect1_Focus2_Accel;
        public float Inspect1_Focus2_Decel;
        // Focus 3 position speeds
        public float Inspect1_Focus3_SpeedZ;
        public float Inspect1_Focus3_SpeedC;
        public float Inspect1_Focus3_Accel;
        public float Inspect1_Focus3_Decel;

        // Inspect 2 Robot parameters (Z4 Camera Focus, C3 PCB Rotate)
        public float Inspect2_Focus1;
        public float Inspect2_Focus2;
        public float Inspect2_Focus3;
        public float Inspect2_Rotate1;
        public float Inspect2_Rotate2;
        public float Inspect2_Rotate3;
        public float Inspect2_IdleZ;
        public float Inspect2_IdleR;
        public float Inspect2_Speed;
        public float Inspect2_AccTime;
        public float Inspect2_DecTime;

        // Inspect 2 Robot individual speeds per step per axis
        // Idle position speeds
        public float Inspect2_Idle_SpeedZ;
        public float Inspect2_Idle_SpeedC;
        public float Inspect2_Idle_Accel;
        public float Inspect2_Idle_Decel;
        // Focus 1 position speeds
        public float Inspect2_Focus1_SpeedZ;
        public float Inspect2_Focus1_SpeedC;
        public float Inspect2_Focus1_Accel;
        public float Inspect2_Focus1_Decel;
        // Focus 2 position speeds
        public float Inspect2_Focus2_SpeedZ;
        public float Inspect2_Focus2_SpeedC;
        public float Inspect2_Focus2_Accel;
        public float Inspect2_Focus2_Decel;
        // Focus 3 position speeds
        public float Inspect2_Focus3_SpeedZ;
        public float Inspect2_Focus3_SpeedC;
        public float Inspect2_Focus3_Accel;
        public float Inspect2_Focus3_Decel;

        // Unload position
        public float Inspect2_UnloadZ;
        public float Inspect2_UnloadC;
        public float Inspect2_Unload_SpeedZ;
        public float Inspect2_Unload_SpeedC;
        public float Inspect2_Unload_Accel;
        public float Inspect2_Unload_Decel;

  public PCBModel()
        {
            // Set default values for legacy speed properties
            PCBInfeedPick_Speed = 1000.0f;
            PCBInfeedPlace_Speed = 1000.0f;
            PCBInfeedPick_Acceleration = 0.1f;
            PCBInfeedPlace_Acceleration = 0.1f;
            PCBInfeedPick_Deceleration = 0.1f;
            PCBInfeedPlace_Deceleration = 0.1f;

            // Infeed Robot default positions
            PCBInfeed_IdleZ = 50.0f;
            PCBInfeed_PickupX = 100.0f;
            PCBInfeed_PickupY = 100.0f;
            PCBInfeed_PickupZ = 10.0f;
            PCBInfeed_PreparePlaceX = 200.0f;
            PCBInfeed_PreparePlaceY = 200.0f;
            PCBInfeed_PreparePlaceZ = 50.0f;
            PCBInfeed_PlaceX = 200.0f;
            PCBInfeed_PlaceY = 200.0f;
            PCBInfeed_PlaceZ = 10.0f;

            // Infeed Robot individual speeds per step (default values)
            PCBInfeed_Idle_SpeedX = 1000.0f;
            PCBInfeed_Idle_SpeedY = 1000.0f;
            PCBInfeed_Idle_SpeedR = 1000.0f;
            PCBInfeed_Idle_Accel = 0.1f;
            PCBInfeed_Idle_Decel = 0.1f;
            PCBInfeed_Pickup_SpeedX = 1000.0f;
            PCBInfeed_Pickup_SpeedY = 1000.0f;
            PCBInfeed_Pickup_SpeedR = 1000.0f;
            PCBInfeed_Pickup_Accel = 0.1f;
            PCBInfeed_Pickup_Decel = 0.1f;
            PCBInfeed_Place_SpeedX = 1000.0f;
            PCBInfeed_Place_SpeedY = 1000.0f;
            PCBInfeed_Place_SpeedR = 1000.0f;
            PCBInfeed_Place_Accel = 0.1f;
            PCBInfeed_Place_Decel = 0.1f;

            // Transfer Robot default positions
            PCBTransfer_IdleX = 0.0f;
            PCBTransfer_IdleZ = 50.0f;
            PCBTransfer_PreparePickupX = 120.0f;
            PCBTransfer_PreparePickupZ = 30.0f;
            PCBTransfer_PickupX = 150.0f;
            PCBTransfer_PickupZ = 10.0f;
            PCBTransfer_PreparePlaceX = 220.0f;
            PCBTransfer_PreparePlaceZ = 30.0f;
            PCBTransfer_PlaceX = 250.0f;
            PCBTransfer_PlaceZ = 10.0f;
            PCBTransfer_NGX = 200.0f;
            PCBTransfer_NGZ = 135.0f;
            PCBTransfer_Speed = 1000.0f;
            PCBTransfer_Acceleration = 0.1f;
            PCBTransfer_Deceleration = 0.1f;

            // Transfer Robot individual speeds per step (default values)
            PCBTransfer_Idle_SpeedX = 1000.0f;
            PCBTransfer_Idle_SpeedZ = 1000.0f;
            PCBTransfer_Idle_Accel = 0.1f;
            PCBTransfer_Idle_Decel = 0.1f;
            PCBTransfer_PreparePickup_SpeedX = 1000.0f;
            PCBTransfer_PreparePickup_SpeedZ = 1000.0f;
            PCBTransfer_PreparePickup_Accel = 0.1f;
            PCBTransfer_PreparePickup_Decel = 0.1f;
            PCBTransfer_Pickup_SpeedX = 1000.0f;
            PCBTransfer_Pickup_SpeedZ = 1000.0f;
            PCBTransfer_Pickup_Accel = 0.1f;
            PCBTransfer_Pickup_Decel = 0.1f;
            PCBTransfer_PreparePlace_SpeedX = 1000.0f;
            PCBTransfer_PreparePlace_SpeedZ = 1000.0f;
            PCBTransfer_PreparePlace_Accel = 0.1f;
            PCBTransfer_PreparePlace_Decel = 0.1f;
            PCBTransfer_Place_SpeedX = 1000.0f;
            PCBTransfer_Place_SpeedZ = 1000.0f;
            PCBTransfer_Place_Accel = 0.1f;
            PCBTransfer_Place_Decel = 0.1f;
            PCBTransfer_NG_SpeedX = 1000.0f;
            PCBTransfer_NG_SpeedZ = 1000.0f;
            PCBTransfer_NG_Accel = 0.1f;
            PCBTransfer_NG_Decel = 0.1f;

            // Outfeed Robot default positions
            PCBOutfeed_IdleZ = 50.0f;
            PCBOutfeed_PickupX = 200.0f;
            PCBOutfeed_PickupY = 200.0f;
            PCBOutfeed_PickupZ = 10.0f;
            PCBOutfeed_PlaceOK1X = 300.0f;
            PCBOutfeed_PlaceOK1Y = 300.0f;
            PCBOutfeed_PlaceOK1Z = 10.0f;
            PCBOutfeed_PlaceOK2X = 310.0f;
            PCBOutfeed_PlaceOK2Y = 310.0f;
            PCBOutfeed_PlaceOK2Z = 10.0f;
            PCBOutfeed_PlaceOK3X = 320.0f;
            PCBOutfeed_PlaceOK3Y = 320.0f;
            PCBOutfeed_PlaceOK3Z = 10.0f;
            PCBOutfeed_PlaceOK4X = 330.0f;
            PCBOutfeed_PlaceOK4Y = 330.0f;
            PCBOutfeed_PlaceOK4Z = 10.0f;
            PCBOutfeed_PlaceOK5X = 340.0f;
            PCBOutfeed_PlaceOK5Y = 340.0f;
            PCBOutfeed_PlaceOK5Z = 10.0f;
            PCBOutfeed_PlaceOK6X = 350.0f;
            PCBOutfeed_PlaceOK6Y = 350.0f;
            PCBOutfeed_PlaceOK6Z = 10.0f;
            PCBOutfeed_PlaceNGX = 400.0f;
            PCBOutfeed_PlaceNGY = 400.0f;
            PCBOutfeed_PlaceNGZ = 10.0f;
            PCBOutfeed_PickupTrayX = 500.0f;
            PCBOutfeed_PickupTrayY = 500.0f;
            PCBOutfeed_PickupTrayZ = 10.0f;
            PCBOutfeed_PlaceTrayX = 600.0f;
            PCBOutfeed_PlaceTrayY = 600.0f;
            PCBOutfeed_PlaceTrayZ = 10.0f;
            PCBOutfeed_PlaceX = 300.0f;
            PCBOutfeed_PlaceY = 300.0f;
            PCBOutfeed_PlaceZ = 10.0f;

            PCBOutfeed_Speed = 1000.0f;
            PCBOutfeed_Acceleration = 0.1f;
            PCBOutfeed_Deceleration = 0.1f;

            // Outfeed Robot individual speeds per step (default values)
            PCBOutfeed_Idle_SpeedX = 1000.0f;
            PCBOutfeed_Idle_SpeedY = 1000.0f;
            PCBOutfeed_Idle_Accel = 0.1f;
            PCBOutfeed_Idle_Decel = 0.1f;
            PCBOutfeed_Pickup_SpeedX = 1000.0f;
            PCBOutfeed_Pickup_SpeedY = 1000.0f;
            PCBOutfeed_Pickup_Accel = 0.1f;
            PCBOutfeed_Pickup_Decel = 0.1f;
            PCBOutfeed_PlaceOK1_SpeedX = 1000.0f;
            PCBOutfeed_PlaceOK1_SpeedY = 1000.0f;
            PCBOutfeed_PlaceOK1_Accel = 0.1f;
            PCBOutfeed_PlaceOK1_Decel = 0.1f;
            PCBOutfeed_PlaceOK2_SpeedX = 1000.0f;
            PCBOutfeed_PlaceOK2_SpeedY = 1000.0f;
            PCBOutfeed_PlaceOK2_Accel = 0.1f;
            PCBOutfeed_PlaceOK2_Decel = 0.1f;
            PCBOutfeed_PlaceOK3_SpeedX = 1000.0f;
            PCBOutfeed_PlaceOK3_SpeedY = 1000.0f;
            PCBOutfeed_PlaceOK3_Accel = 0.1f;
            PCBOutfeed_PlaceOK3_Decel = 0.1f;
            PCBOutfeed_PlaceOK4_SpeedX = 1000.0f;
            PCBOutfeed_PlaceOK4_SpeedY = 1000.0f;
            PCBOutfeed_PlaceOK4_Accel = 0.1f;
            PCBOutfeed_PlaceOK4_Decel = 0.1f;
            PCBOutfeed_PlaceOK5_SpeedX = 1000.0f;
            PCBOutfeed_PlaceOK5_SpeedY = 1000.0f;
            PCBOutfeed_PlaceOK5_Accel = 0.1f;
            PCBOutfeed_PlaceOK5_Decel = 0.1f;
            PCBOutfeed_PlaceOK6_SpeedX = 1000.0f;
            PCBOutfeed_PlaceOK6_SpeedY = 1000.0f;
            PCBOutfeed_PlaceOK6_Accel = 0.1f;
            PCBOutfeed_PlaceOK6_Decel = 0.1f;
            PCBOutfeed_PlaceNG_SpeedX = 1000.0f;
            PCBOutfeed_PlaceNG_SpeedY = 1000.0f;
            PCBOutfeed_PlaceNG_Accel = 0.1f;
            PCBOutfeed_PlaceNG_Decel = 0.1f;
            PCBOutfeed_PickupTray_SpeedX = 1000.0f;
            PCBOutfeed_PickupTray_SpeedY = 1000.0f;
            PCBOutfeed_PickupTray_Accel = 0.1f;
            PCBOutfeed_PickupTray_Decel = 0.1f;
            PCBOutfeed_PlaceTray_SpeedX = 1000.0f;
            PCBOutfeed_PlaceTray_SpeedY = 1000.0f;
            PCBOutfeed_PlaceTray_Accel = 0.1f;
            PCBOutfeed_PlaceTray_Decel = 0.1f;

         // Inspect 1 Robot default parameters
     Inspect1_Focus1 = 0.0f;
            Inspect1_Focus2 = 5.0f;
 Inspect1_Focus3 = 10.0f;
            Inspect1_Rotate1 = 0.0f;
            Inspect1_Rotate2 = 90.0f;
      Inspect1_Rotate3 = 180.0f;
            Inspect1_IdleZ = 0.0f;
     Inspect1_IdleR = 0.0f;
         Inspect1_Speed = 1000.0f;
Inspect1_AccTime = 0.1f;
         Inspect1_DecTime = 0.1f;

            // Inspect 1 Robot individual speeds per step (default values)
       Inspect1_Idle_SpeedZ = 1000.0f;
            Inspect1_Idle_SpeedC = 1000.0f;
       Inspect1_Idle_Accel = 0.1f;
            Inspect1_Idle_Decel = 0.1f;
            Inspect1_Focus1_SpeedZ = 1000.0f;
  Inspect1_Focus1_SpeedC = 1000.0f;
          Inspect1_Focus1_Accel = 0.1f;
     Inspect1_Focus1_Decel = 0.1f;
            Inspect1_Focus2_SpeedZ = 1000.0f;
            Inspect1_Focus2_SpeedC = 1000.0f;
     Inspect1_Focus2_Accel = 0.1f;
            Inspect1_Focus2_Decel = 0.1f;
        Inspect1_Focus3_SpeedZ = 1000.0f;
    Inspect1_Focus3_SpeedC = 1000.0f;
            Inspect1_Focus3_Accel = 0.1f;
         Inspect1_Focus3_Decel = 0.1f;

            // Inspect 2 Robot default parameters
            Inspect2_Focus1 = 0.0f;
    Inspect2_Focus2 = 5.0f;
    Inspect2_Focus3 = 10.0f;
  Inspect2_Rotate1 = 0.0f;
         Inspect2_Rotate2 = 90.0f;
            Inspect2_Rotate3 = 180.0f;
      Inspect2_IdleZ = 0.0f;
  Inspect2_IdleR = 0.0f;
            Inspect2_Speed = 1000.0f;
      Inspect2_AccTime = 0.1f;
  Inspect2_DecTime = 0.1f;

    // Inspect 2 Robot individual speeds per step (default values)
Inspect2_Idle_SpeedZ = 1000.0f;
            Inspect2_Idle_SpeedC = 1000.0f;
    Inspect2_Idle_Accel = 0.1f;
 Inspect2_Idle_Decel = 0.1f;
        Inspect2_Focus1_SpeedZ = 1000.0f;
 Inspect2_Focus1_SpeedC = 1000.0f;
         Inspect2_Focus1_Accel = 0.1f;
         Inspect2_Focus1_Decel = 0.1f;
            Inspect2_Focus2_SpeedZ = 1000.0f;
    Inspect2_Focus2_SpeedC = 1000.0f;
  Inspect2_Focus2_Accel = 0.1f;
    Inspect2_Focus2_Decel = 0.1f;
    Inspect2_Focus3_SpeedZ = 1000.0f;
            Inspect2_Focus3_SpeedC = 1000.0f;
    Inspect2_Focus3_Accel = 0.1f;
     Inspect2_Focus3_Decel = 0.1f;

            // Inspect 2 Robot Unload position (default values)
            Inspect2_UnloadZ = 0.0f;
            Inspect2_UnloadC = 0.0f;
            Inspect2_Unload_SpeedZ = 1000.0f;
            Inspect2_Unload_SpeedC = 1000.0f;
            Inspect2_Unload_Accel = 0.1f;
            Inspect2_Unload_Decel = 0.1f;

       // Initialize vision solution paths
   VisionSolutionName = "Default.SOL";
        VisionSolutionPath = System.IO.Path.Combine(@"E:\VMSolution", VisionSolutionName);
  }

        /// <summary>
        /// Get the full path to the vision solution file for this model
        /// </summary>
     /// <returns>Full path to the .SOL file</returns>
public string GetVisionSolutionFullPath()
        {
            if (string.IsNullOrEmpty(VisionSolutionPath))
            {
                return System.IO.Path.Combine(@"E:\VMSolution", VisionSolutionName ?? "Default.SOL");
            }
            return VisionSolutionPath;
        }

        /// <summary>
        /// Set the vision solution for this model with automatic path generation
        /// </summary>
        /// <param name="solutionName">Name of the solution file (with .SOL extension)</param>
        public void SetVisionSolution(string solutionName)
        {
            VisionSolutionName = solutionName;
            VisionSolutionPath = System.IO.Path.Combine(@"E:\VMSolution", solutionName);
        }

        /// <summary>
        /// Get the directory path where vision solutions are stored for this model
        /// </summary>
        /// <returns>Directory path for model-specific vision solutions</returns>
        public string GetModelVisionDirectory()
        {
            string modelDirName = $"Model_{Id}_{Name?.Replace(" ", "_").Replace("\\", "").Replace("/", "")}";
            return System.IO.Path.Combine(@"E:\VMSolution", modelDirName);
        }

        public PCBModel Clone()
        {
            return new PCBModel
            {
                Id = 0, // New ID for clone
                Name = $"{Name} - Copy",
                Description = Description,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                IsActive = false,

                // Copy vision solution settings
                VisionSolutionPath = VisionSolutionPath,
                VisionSolutionName = VisionSolutionName,

                PCBInfeedPick_Z = PCBInfeedPick_Z,
                PCBInfeedPlace_Z = PCBInfeedPlace_Z,
                PCBInfeedPick_Speed = PCBInfeedPick_Speed,
                PCBInfeedPlace_Speed = PCBInfeedPlace_Speed,
                PCBInfeedPick_Acceleration = PCBInfeedPick_Acceleration,
                PCBInfeedPlace_Acceleration = PCBInfeedPlace_Acceleration,
                PCBInfeedPick_Deceleration = PCBInfeedPick_Deceleration,
                PCBInfeedPlace_Deceleration = PCBInfeedPlace_Deceleration,

                PCBInfeed_IdleX = PCBInfeed_IdleX,
                PCBInfeed_IdleY = PCBInfeed_IdleY,
                PCBInfeed_IdleZ = PCBInfeed_IdleZ,
                PCBInfeed_IdleR = PCBInfeed_IdleR,

                PCBInfeed_PickupX = PCBInfeed_PickupX,
                PCBInfeed_PickupY = PCBInfeed_PickupY,
                PCBInfeed_PickupZ = PCBInfeed_PickupZ,
                PCBInfeed_PickupR = PCBInfeed_PickupR,

                PCBInfeed_PreparePlaceX = PCBInfeed_PreparePlaceX,
                PCBInfeed_PreparePlaceY = PCBInfeed_PreparePlaceY,
                PCBInfeed_PreparePlaceZ = PCBInfeed_PreparePlaceZ,
                PCBInfeed_PreparePlaceR = PCBInfeed_PreparePlaceR,

                PCBInfeed_PlaceX = PCBInfeed_PlaceX,
                PCBInfeed_PlaceY = PCBInfeed_PlaceY,
                PCBInfeed_PlaceZ = PCBInfeed_PlaceZ,
                PCBInfeed_PlaceR = PCBInfeed_PlaceR,

                // Copy Infeed individual speeds
                PCBInfeed_Idle_SpeedX = PCBInfeed_Idle_SpeedX,
                PCBInfeed_Idle_SpeedY = PCBInfeed_Idle_SpeedY,
                PCBInfeed_Idle_SpeedR = PCBInfeed_Idle_SpeedR,
                PCBInfeed_Idle_Accel = PCBInfeed_Idle_Accel,
                PCBInfeed_Idle_Decel = PCBInfeed_Idle_Decel,
                PCBInfeed_Pickup_SpeedX = PCBInfeed_Pickup_SpeedX,
                PCBInfeed_Pickup_SpeedY = PCBInfeed_Pickup_SpeedY,
                PCBInfeed_Pickup_SpeedR = PCBInfeed_Pickup_SpeedR,
                PCBInfeed_Pickup_Accel = PCBInfeed_Pickup_Accel,
                PCBInfeed_Pickup_Decel = PCBInfeed_Pickup_Decel,
                PCBInfeed_Place_SpeedX = PCBInfeed_Place_SpeedX,
                PCBInfeed_Place_SpeedY = PCBInfeed_Place_SpeedY,
                PCBInfeed_Place_SpeedR = PCBInfeed_Place_SpeedR,
                PCBInfeed_Place_Accel = PCBInfeed_Place_Accel,
                PCBInfeed_Place_Decel = PCBInfeed_Place_Decel,

                // Copy Transfer Robot settings
                PCBTransfer_IdleX = PCBTransfer_IdleX,
                PCBTransfer_IdleZ = PCBTransfer_IdleZ,
                PCBTransfer_PreparePickupX = PCBTransfer_PreparePickupX,
                PCBTransfer_PreparePickupZ = PCBTransfer_PreparePickupZ,
                PCBTransfer_PickupX = PCBTransfer_PickupX,
                PCBTransfer_PickupZ = PCBTransfer_PickupZ,
                PCBTransfer_PreparePlaceX = PCBTransfer_PreparePlaceX,
                PCBTransfer_PreparePlaceZ = PCBTransfer_PreparePlaceZ,
                PCBTransfer_PlaceX = PCBTransfer_PlaceX,
                PCBTransfer_PlaceZ = PCBTransfer_PlaceZ,
                PCBTransfer_NGX = PCBTransfer_NGX,
                PCBTransfer_NGZ = PCBTransfer_NGZ,
                PCBTransfer_Speed = PCBTransfer_Speed,
                PCBTransfer_Acceleration = PCBTransfer_Acceleration,
                PCBTransfer_Deceleration = PCBTransfer_Deceleration,

                // Copy Transfer individual speeds
                PCBTransfer_Idle_SpeedX = PCBTransfer_Idle_SpeedX,
                PCBTransfer_Idle_SpeedZ = PCBTransfer_Idle_SpeedZ,
                PCBTransfer_Idle_Accel = PCBTransfer_Idle_Accel,
                PCBTransfer_Idle_Decel = PCBTransfer_Idle_Decel,
                PCBTransfer_PreparePickup_SpeedX = PCBTransfer_PreparePickup_SpeedX,
                PCBTransfer_PreparePickup_SpeedZ = PCBTransfer_PreparePickup_SpeedZ,
                PCBTransfer_PreparePickup_Accel = PCBTransfer_PreparePickup_Accel,
                PCBTransfer_PreparePickup_Decel = PCBTransfer_PreparePickup_Decel,
                PCBTransfer_Pickup_SpeedX = PCBTransfer_Pickup_SpeedX,
                PCBTransfer_Pickup_SpeedZ = PCBTransfer_Pickup_SpeedZ,
                PCBTransfer_Pickup_Accel = PCBTransfer_Pickup_Accel,
                PCBTransfer_Pickup_Decel = PCBTransfer_Pickup_Decel,
                PCBTransfer_PreparePlace_SpeedX = PCBTransfer_PreparePlace_SpeedX,
                PCBTransfer_PreparePlace_SpeedZ = PCBTransfer_PreparePlace_SpeedZ,
                PCBTransfer_PreparePlace_Accel = PCBTransfer_PreparePlace_Accel,
                PCBTransfer_PreparePlace_Decel = PCBTransfer_PreparePlace_Decel,
                PCBTransfer_Place_SpeedX = PCBTransfer_Place_SpeedX,
                PCBTransfer_Place_SpeedZ = PCBTransfer_Place_SpeedZ,
                PCBTransfer_Place_Accel = PCBTransfer_Place_Accel,
                PCBTransfer_Place_Decel = PCBTransfer_Place_Decel,
                PCBTransfer_NG_SpeedX = PCBTransfer_NG_SpeedX,
                PCBTransfer_NG_SpeedZ = PCBTransfer_NG_SpeedZ,
                PCBTransfer_NG_Accel = PCBTransfer_NG_Accel,
                PCBTransfer_NG_Decel = PCBTransfer_NG_Decel,

                PCBOutfeed_IdleX = PCBOutfeed_IdleX,
                PCBOutfeed_IdleY = PCBOutfeed_IdleY,
                PCBOutfeed_IdleZ = PCBOutfeed_IdleZ,

                PCBOutfeed_PickupX = PCBOutfeed_PickupX,
                PCBOutfeed_PickupY = PCBOutfeed_PickupY,
                PCBOutfeed_PickupZ = PCBOutfeed_PickupZ,

                PCBOutfeed_PlaceOK1X = PCBOutfeed_PlaceOK1X,
                PCBOutfeed_PlaceOK1Y = PCBOutfeed_PlaceOK1Y,
                PCBOutfeed_PlaceOK1Z = PCBOutfeed_PlaceOK1Z,
                PCBOutfeed_PlaceOK2X = PCBOutfeed_PlaceOK2X,
                PCBOutfeed_PlaceOK2Y = PCBOutfeed_PlaceOK2Y,
                PCBOutfeed_PlaceOK2Z = PCBOutfeed_PlaceOK2Z,
                PCBOutfeed_PlaceOK3X = PCBOutfeed_PlaceOK3X,
                PCBOutfeed_PlaceOK3Y = PCBOutfeed_PlaceOK3Y,
                PCBOutfeed_PlaceOK3Z = PCBOutfeed_PlaceOK3Z,
                PCBOutfeed_PlaceOK4X = PCBOutfeed_PlaceOK4X,
                PCBOutfeed_PlaceOK4Y = PCBOutfeed_PlaceOK4Y,
                PCBOutfeed_PlaceOK4Z = PCBOutfeed_PlaceOK4Z,
                PCBOutfeed_PlaceOK5X = PCBOutfeed_PlaceOK5X,
                PCBOutfeed_PlaceOK5Y = PCBOutfeed_PlaceOK5Y,
                PCBOutfeed_PlaceOK5Z = PCBOutfeed_PlaceOK5Z,
                PCBOutfeed_PlaceOK6X = PCBOutfeed_PlaceOK6X,
                PCBOutfeed_PlaceOK6Y = PCBOutfeed_PlaceOK6Y,
                PCBOutfeed_PlaceOK6Z = PCBOutfeed_PlaceOK6Z,

                PCBOutfeed_PlaceNGX = PCBOutfeed_PlaceNGX,
                PCBOutfeed_PlaceNGY = PCBOutfeed_PlaceNGY,
                PCBOutfeed_PlaceNGZ = PCBOutfeed_PlaceNGZ,

                PCBOutfeed_PickupTrayX = PCBOutfeed_PickupTrayX,
                PCBOutfeed_PickupTrayY = PCBOutfeed_PickupTrayY,
                PCBOutfeed_PickupTrayZ = PCBOutfeed_PickupTrayZ,

                PCBOutfeed_PlaceTrayX = PCBOutfeed_PlaceTrayX,
                PCBOutfeed_PlaceTrayY = PCBOutfeed_PlaceTrayY,
                PCBOutfeed_PlaceTrayZ = PCBOutfeed_PlaceTrayZ,

                PCBOutfeed_PlaceX = PCBOutfeed_PlaceX,
                PCBOutfeed_PlaceY = PCBOutfeed_PlaceY,
                PCBOutfeed_PlaceZ = PCBOutfeed_PlaceZ,

                PCBOutfeed_Speed = PCBOutfeed_Speed,
                PCBOutfeed_Acceleration = PCBOutfeed_Acceleration,
                PCBOutfeed_Deceleration = PCBOutfeed_Deceleration,

                // Copy Outfeed individual speeds
                PCBOutfeed_Idle_SpeedX = PCBOutfeed_Idle_SpeedX,
                PCBOutfeed_Idle_SpeedY = PCBOutfeed_Idle_SpeedY,
                PCBOutfeed_Idle_Accel = PCBOutfeed_Idle_Accel,
                PCBOutfeed_Idle_Decel = PCBOutfeed_Idle_Decel,
                PCBOutfeed_Pickup_SpeedX = PCBOutfeed_Pickup_SpeedX,
                PCBOutfeed_Pickup_SpeedY = PCBOutfeed_Pickup_SpeedY,
                PCBOutfeed_Pickup_Accel = PCBOutfeed_Pickup_Accel,
                PCBOutfeed_Pickup_Decel = PCBOutfeed_Pickup_Decel,
                PCBOutfeed_PlaceOK1_SpeedX = PCBOutfeed_PlaceOK1_SpeedX,
                PCBOutfeed_PlaceOK1_SpeedY = PCBOutfeed_PlaceOK1_SpeedY,
                PCBOutfeed_PlaceOK1_Accel = PCBOutfeed_PlaceOK1_Accel,
                PCBOutfeed_PlaceOK1_Decel = PCBOutfeed_PlaceOK1_Decel,
                PCBOutfeed_PlaceOK2_SpeedX = PCBOutfeed_PlaceOK2_SpeedX,
                PCBOutfeed_PlaceOK2_SpeedY = PCBOutfeed_PlaceOK2_SpeedY,
                PCBOutfeed_PlaceOK2_Accel = PCBOutfeed_PlaceOK2_Accel,
                PCBOutfeed_PlaceOK2_Decel = PCBOutfeed_PlaceOK2_Decel,
                PCBOutfeed_PlaceOK3_SpeedX = PCBOutfeed_PlaceOK3_SpeedX,
                PCBOutfeed_PlaceOK3_SpeedY = PCBOutfeed_PlaceOK3_SpeedY,
                PCBOutfeed_PlaceOK3_Accel = PCBOutfeed_PlaceOK3_Accel,
                PCBOutfeed_PlaceOK3_Decel = PCBOutfeed_PlaceOK3_Decel,
                PCBOutfeed_PlaceOK4_SpeedX = PCBOutfeed_PlaceOK4_SpeedX,
                PCBOutfeed_PlaceOK4_SpeedY = PCBOutfeed_PlaceOK4_SpeedY,
                PCBOutfeed_PlaceOK4_Accel = PCBOutfeed_PlaceOK4_Accel,
                PCBOutfeed_PlaceOK4_Decel = PCBOutfeed_PlaceOK4_Decel,
                PCBOutfeed_PlaceOK5_SpeedX = PCBOutfeed_PlaceOK5_SpeedX,
                PCBOutfeed_PlaceOK5_SpeedY = PCBOutfeed_PlaceOK5_SpeedY,
                PCBOutfeed_PlaceOK5_Accel = PCBOutfeed_PlaceOK5_Accel,
                PCBOutfeed_PlaceOK5_Decel = PCBOutfeed_PlaceOK5_Decel,
                PCBOutfeed_PlaceOK6_SpeedX = PCBOutfeed_PlaceOK6_SpeedX,
                PCBOutfeed_PlaceOK6_SpeedY = PCBOutfeed_PlaceOK6_SpeedY,
                PCBOutfeed_PlaceOK6_Accel = PCBOutfeed_PlaceOK6_Accel,
                PCBOutfeed_PlaceOK6_Decel = PCBOutfeed_PlaceOK6_Decel,
                PCBOutfeed_PlaceNG_SpeedX = PCBOutfeed_PlaceNG_SpeedX,
                PCBOutfeed_PlaceNG_SpeedY = PCBOutfeed_PlaceNG_SpeedY,
                PCBOutfeed_PlaceNG_Accel = PCBOutfeed_PlaceNG_Accel,
                PCBOutfeed_PlaceNG_Decel = PCBOutfeed_PlaceNG_Decel,
                PCBOutfeed_PickupTray_SpeedX = PCBOutfeed_PickupTray_SpeedX,
                PCBOutfeed_PickupTray_SpeedY = PCBOutfeed_PickupTray_SpeedY,
                PCBOutfeed_PickupTray_Accel = PCBOutfeed_PickupTray_Accel,
                PCBOutfeed_PickupTray_Decel = PCBOutfeed_PickupTray_Decel,
                PCBOutfeed_PlaceTray_SpeedX = PCBOutfeed_PlaceTray_SpeedX,
                PCBOutfeed_PlaceTray_SpeedY = PCBOutfeed_PlaceTray_SpeedY,
                PCBOutfeed_PlaceTray_Accel = PCBOutfeed_PlaceTray_Accel,
                PCBOutfeed_PlaceTray_Decel = PCBOutfeed_PlaceTray_Decel,

                // Copy Inspect 1 Robot settings
                Inspect1_Focus1 = Inspect1_Focus1,
                Inspect1_Focus2 = Inspect1_Focus2,
                Inspect1_Focus3 = Inspect1_Focus3,
                Inspect1_Rotate1 = Inspect1_Rotate1,
                Inspect1_Rotate2 = Inspect1_Rotate2,
                Inspect1_Rotate3 = Inspect1_Rotate3,
                Inspect1_IdleZ = Inspect1_IdleZ,
                Inspect1_IdleR = Inspect1_IdleR,
                Inspect1_Speed = Inspect1_Speed,
                Inspect1_AccTime = Inspect1_AccTime,
                Inspect1_DecTime = Inspect1_DecTime,

                // Copy Inspect 1 individual speeds
                Inspect1_Idle_SpeedZ = Inspect1_Idle_SpeedZ,
                Inspect1_Idle_SpeedC = Inspect1_Idle_SpeedC,
                Inspect1_Idle_Accel = Inspect1_Idle_Accel,
                Inspect1_Idle_Decel = Inspect1_Idle_Decel,
                Inspect1_Focus1_SpeedZ = Inspect1_Focus1_SpeedZ,
                Inspect1_Focus1_SpeedC = Inspect1_Focus1_SpeedC,
                Inspect1_Focus1_Accel = Inspect1_Focus1_Accel,
                Inspect1_Focus1_Decel = Inspect1_Focus1_Decel,
                Inspect1_Focus2_SpeedZ = Inspect1_Focus2_SpeedZ,
                Inspect1_Focus2_SpeedC = Inspect1_Focus2_SpeedC,
                Inspect1_Focus2_Accel = Inspect1_Focus2_Accel,
                Inspect1_Focus2_Decel = Inspect1_Focus2_Decel,
                Inspect1_Focus3_SpeedZ = Inspect1_Focus3_SpeedZ,
                Inspect1_Focus3_SpeedC = Inspect1_Focus3_SpeedC,
                Inspect1_Focus3_Accel = Inspect1_Focus3_Accel,
                Inspect1_Focus3_Decel = Inspect1_Focus3_Decel,

                // Copy Inspect 2 Robot settings
                Inspect2_Focus1 = Inspect2_Focus1,
                Inspect2_Focus2 = Inspect2_Focus2,
                Inspect2_Focus3 = Inspect2_Focus3,
                Inspect2_Rotate1 = Inspect2_Rotate1,
                Inspect2_Rotate2 = Inspect2_Rotate2,
                Inspect2_Rotate3 = Inspect2_Rotate3,
                Inspect2_IdleZ = Inspect2_IdleZ,
                Inspect2_IdleR = Inspect2_IdleR,
                Inspect2_Speed = Inspect2_Speed,
                Inspect2_AccTime = Inspect2_AccTime,
                Inspect2_DecTime = Inspect2_DecTime,

                // Copy Inspect 2 individual speeds
                Inspect2_Idle_SpeedZ = Inspect2_Idle_SpeedZ,
                Inspect2_Idle_SpeedC = Inspect2_Idle_SpeedC,
                Inspect2_Idle_Accel = Inspect2_Idle_Accel,
                Inspect2_Idle_Decel = Inspect2_Idle_Decel,
                Inspect2_Focus1_SpeedZ = Inspect2_Focus1_SpeedZ,
                Inspect2_Focus1_SpeedC = Inspect2_Focus1_SpeedC,
                Inspect2_Focus1_Accel = Inspect2_Focus1_Accel,
                Inspect2_Focus1_Decel = Inspect2_Focus1_Decel,
                Inspect2_Focus2_SpeedZ = Inspect2_Focus2_SpeedZ,
                Inspect2_Focus2_SpeedC = Inspect2_Focus2_SpeedC,
                Inspect2_Focus2_Accel = Inspect2_Focus2_Accel,
                Inspect2_Focus2_Decel = Inspect2_Focus2_Decel,
                Inspect2_Focus3_SpeedZ = Inspect2_Focus3_SpeedZ,
                Inspect2_Focus3_SpeedC = Inspect2_Focus3_SpeedC,
                Inspect2_Focus3_Accel = Inspect2_Focus3_Accel,
                Inspect2_Focus3_Decel = Inspect2_Focus3_Decel,

                // Copy Inspect 2 Unload position
                Inspect2_UnloadZ = Inspect2_UnloadZ,
                Inspect2_UnloadC = Inspect2_UnloadC,
                Inspect2_Unload_SpeedZ = Inspect2_Unload_SpeedZ,
                Inspect2_Unload_SpeedC = Inspect2_Unload_SpeedC,
                Inspect2_Unload_Accel = Inspect2_Unload_Accel,
                Inspect2_Unload_Decel = Inspect2_Unload_Decel
            };
        }
    }
}



