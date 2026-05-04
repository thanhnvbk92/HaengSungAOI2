using System.Collections.ObjectModel;
using System.Linq;
using HaengSungAOI_WPF.Models;
using HaengSungAOI_WPF.Services.Machine;

namespace HaengSungAOI_WPF.ViewModels
{
    /// <summary>
    /// Manages robot position collections and provides operations for loading/saving
    /// </summary>
    public class RobotPositionManager
    {
        public ObservableCollection<RobotPositionEntry> InfeedPositions { get; private set; }
        public ObservableCollection<RobotPositionEntry> TransferPositions { get; private set; }
        public ObservableCollection<RobotPositionEntry> OutfeedPositions { get; private set; }
        public ObservableCollection<RobotPositionEntry> Inspect1Positions { get; private set; }
        public ObservableCollection<RobotPositionEntry> Inspect2Positions { get; private set; }

        public RobotPositionManager()
        {
            InitializeCollections();
        }

        private void InitializeCollections()
        {
            // Initialize Infeed Robot Positions (3-axis: X, Y, R)
            InfeedPositions = new ObservableCollection<RobotPositionEntry>
            {
                new RobotPositionEntry { Position = "Idle" },
                new RobotPositionEntry { Position = "Pickup" },
                new RobotPositionEntry { Position = "Place" }
            };

            // Initialize Transfer Robot Positions (2-axis: X, Z)
            TransferPositions = new ObservableCollection<RobotPositionEntry>
            {
                new RobotPositionEntry { Position = "Idle" },
                new RobotPositionEntry { Position = "Prepare Pickup" },
                new RobotPositionEntry { Position = "Pickup" },
                new RobotPositionEntry { Position = "Prepare Place" },
                new RobotPositionEntry { Position = "Place" },
                new RobotPositionEntry { Position = "NG Position" }
            };

            // Initialize Outfeed Robot Positions (2-axis: X, Y)
            OutfeedPositions = new ObservableCollection<RobotPositionEntry>
            {
                new RobotPositionEntry { Position = "Idle" },
                new RobotPositionEntry { Position = "Pickup" },
                new RobotPositionEntry { Position = "OK Place 1" },
                new RobotPositionEntry { Position = "OK Place 2" },
                new RobotPositionEntry { Position = "OK Place 3" },
                new RobotPositionEntry { Position = "OK Place 4" },
                new RobotPositionEntry { Position = "OK Place 5" },
                new RobotPositionEntry { Position = "OK Place 6" },
                new RobotPositionEntry { Position = "NG Place" },
                new RobotPositionEntry { Position = "Pickup Tray" },
                new RobotPositionEntry { Position = "Place Tray" }
            };

            // Initialize Inspect 1 Robot Positions (2-axis: Z, C)
            Inspect1Positions = new ObservableCollection<RobotPositionEntry>
            {
                new RobotPositionEntry { Position = "Idle" },
                new RobotPositionEntry { Position = "Focus 1" },
                new RobotPositionEntry { Position = "Focus 2" },
                new RobotPositionEntry { Position = "Focus 3" }
            };

            // Initialize Inspect 2 Robot Positions (2-axis: Z, C)
            Inspect2Positions = new ObservableCollection<RobotPositionEntry>
            {
                new RobotPositionEntry { Position = "Idle" },
                new RobotPositionEntry { Position = "Focus 1" },
                new RobotPositionEntry { Position = "Focus 2" },
                new RobotPositionEntry { Position = "Focus 3" },
                new RobotPositionEntry { Position = "Unload" }
            };
        }

        #region Load Operations

        public void LoadInfeedPositions(PCBModel model)
        {
            var idle = InfeedPositions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = model.PCBInfeed_IdleX;
                idle.Y = model.PCBInfeed_IdleY;
                idle.R = model.PCBInfeed_IdleR;
                idle.SpeedX = model.PCBInfeed_Idle_SpeedX;
                idle.SpeedY = model.PCBInfeed_Idle_SpeedY;
                idle.SpeedR = model.PCBInfeed_Idle_SpeedR;
                idle.Accel = model.PCBInfeed_Idle_Accel;
                idle.Decel = model.PCBInfeed_Idle_Decel;
            }

            var pickup = InfeedPositions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = model.PCBInfeed_PickupX;
                pickup.Y = model.PCBInfeed_PickupY;
                pickup.R = model.PCBInfeed_PickupR;
                pickup.SpeedX = model.PCBInfeed_Pickup_SpeedX;
                pickup.SpeedY = model.PCBInfeed_Pickup_SpeedY;
                pickup.SpeedR = model.PCBInfeed_Pickup_SpeedR;
                pickup.Accel = model.PCBInfeed_Pickup_Accel;
                pickup.Decel = model.PCBInfeed_Pickup_Decel;
            }

            var place = InfeedPositions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                place.X = model.PCBInfeed_PlaceX;
                place.Y = model.PCBInfeed_PlaceY;
                place.R = model.PCBInfeed_PlaceR;
                place.SpeedX = model.PCBInfeed_Place_SpeedX;
                place.SpeedY = model.PCBInfeed_Place_SpeedY;
                place.SpeedR = model.PCBInfeed_Place_SpeedR;
                place.Accel = model.PCBInfeed_Place_Accel;
                place.Decel = model.PCBInfeed_Place_Decel;
            }
        }

        public void LoadTransferPositions(PCBModel model)
        {
            var idle = TransferPositions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = model.PCBTransfer_IdleX;
                idle.Z = model.PCBTransfer_IdleZ;
                idle.SpeedX = model.PCBTransfer_Idle_SpeedX;
                idle.SpeedZ = model.PCBTransfer_Idle_SpeedZ;
                idle.Accel = model.PCBTransfer_Idle_Accel;
                idle.Decel = model.PCBTransfer_Idle_Decel;
            }

            var preparePickup = TransferPositions.FirstOrDefault(p => p.Position == "Prepare Pickup");
            if (preparePickup != null)
            {
                preparePickup.X = model.PCBTransfer_PreparePickupX;
                preparePickup.Z = model.PCBTransfer_PreparePickupZ;
                preparePickup.SpeedX = model.PCBTransfer_PreparePickup_SpeedX;
                preparePickup.SpeedZ = model.PCBTransfer_PreparePickup_SpeedZ;
                preparePickup.Accel = model.PCBTransfer_PreparePickup_Accel;
                preparePickup.Decel = model.PCBTransfer_PreparePickup_Decel;
            }

            var pickup = TransferPositions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = model.PCBTransfer_PickupX;
                pickup.Z = model.PCBTransfer_PickupZ;
                pickup.SpeedX = model.PCBTransfer_Pickup_SpeedX;
                pickup.SpeedZ = model.PCBTransfer_Pickup_SpeedZ;
                pickup.Accel = model.PCBTransfer_Pickup_Accel;
                pickup.Decel = model.PCBTransfer_Pickup_Decel;
            }

            var preparePlace = TransferPositions.FirstOrDefault(p => p.Position == "Prepare Place");
            if (preparePlace != null)
            {
                preparePlace.X = model.PCBTransfer_PreparePlaceX;
                preparePlace.Z = model.PCBTransfer_PreparePlaceZ;
                preparePlace.SpeedX = model.PCBTransfer_PreparePlace_SpeedX;
                preparePlace.SpeedZ = model.PCBTransfer_PreparePlace_SpeedZ;
                preparePlace.Accel = model.PCBTransfer_PreparePlace_Accel;
                preparePlace.Decel = model.PCBTransfer_PreparePlace_Decel;
            }

            var place = TransferPositions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                place.X = model.PCBTransfer_PlaceX;
                place.Z = model.PCBTransfer_PlaceZ;
                place.SpeedX = model.PCBTransfer_Place_SpeedX;
                place.SpeedZ = model.PCBTransfer_Place_SpeedZ;
                place.Accel = model.PCBTransfer_Place_Accel;
                place.Decel = model.PCBTransfer_Place_Decel;
            }

            var ng = TransferPositions.FirstOrDefault(p => p.Position == "NG Position");
            if (ng != null)
            {
                ng.X = model.PCBTransfer_NGX;
                ng.Z = model.PCBTransfer_NGZ;
                ng.SpeedX = model.PCBTransfer_NG_SpeedX;
                ng.SpeedZ = model.PCBTransfer_NG_SpeedZ;
                ng.Accel = model.PCBTransfer_NG_Accel;
                ng.Decel = model.PCBTransfer_NG_Decel;
            }
        }

        public void LoadOutfeedPositions(PCBModel model)
        {
            var idle = OutfeedPositions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.X = model.PCBOutfeed_IdleX;
                idle.Y = model.PCBOutfeed_IdleY;
                idle.SpeedX = model.PCBOutfeed_Idle_SpeedX;
                idle.SpeedY = model.PCBOutfeed_Idle_SpeedY;
                idle.Accel = model.PCBOutfeed_Idle_Accel;
                idle.Decel = model.PCBOutfeed_Idle_Decel;
            }

            var pickup = OutfeedPositions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                pickup.X = model.PCBOutfeed_PickupX;
                pickup.Y = model.PCBOutfeed_PickupY;
                pickup.SpeedX = model.PCBOutfeed_Pickup_SpeedX;
                pickup.SpeedY = model.PCBOutfeed_Pickup_SpeedY;
                pickup.Accel = model.PCBOutfeed_Pickup_Accel;
                pickup.Decel = model.PCBOutfeed_Pickup_Decel;
            }

            // OK Place positions
            LoadOutfeedOKPlacePosition(model, "OK Place 1",
                model.PCBOutfeed_PlaceOK1X, model.PCBOutfeed_PlaceOK1Y,
                model.PCBOutfeed_PlaceOK1_SpeedX, model.PCBOutfeed_PlaceOK1_SpeedY,
                model.PCBOutfeed_PlaceOK1_Accel, model.PCBOutfeed_PlaceOK1_Decel);

            LoadOutfeedOKPlacePosition(model, "OK Place 2",
                model.PCBOutfeed_PlaceOK2X, model.PCBOutfeed_PlaceOK2Y,
                model.PCBOutfeed_PlaceOK2_SpeedX, model.PCBOutfeed_PlaceOK2_SpeedY,
                model.PCBOutfeed_PlaceOK2_Accel, model.PCBOutfeed_PlaceOK2_Decel);

            LoadOutfeedOKPlacePosition(model, "OK Place 3",
                model.PCBOutfeed_PlaceOK3X, model.PCBOutfeed_PlaceOK3Y,
                model.PCBOutfeed_PlaceOK3_SpeedX, model.PCBOutfeed_PlaceOK3_SpeedY,
                model.PCBOutfeed_PlaceOK3_Accel, model.PCBOutfeed_PlaceOK3_Decel);

            LoadOutfeedOKPlacePosition(model, "OK Place 4",
                model.PCBOutfeed_PlaceOK4X, model.PCBOutfeed_PlaceOK4Y,
                model.PCBOutfeed_PlaceOK4_SpeedX, model.PCBOutfeed_PlaceOK4_SpeedY,
                model.PCBOutfeed_PlaceOK4_Accel, model.PCBOutfeed_PlaceOK4_Decel);

            LoadOutfeedOKPlacePosition(model, "OK Place 5",
                model.PCBOutfeed_PlaceOK5X, model.PCBOutfeed_PlaceOK5Y,
                model.PCBOutfeed_PlaceOK5_SpeedX, model.PCBOutfeed_PlaceOK5_SpeedY,
                model.PCBOutfeed_PlaceOK5_Accel, model.PCBOutfeed_PlaceOK5_Decel);

            LoadOutfeedOKPlacePosition(model, "OK Place 6",
                model.PCBOutfeed_PlaceOK6X, model.PCBOutfeed_PlaceOK6Y,
                model.PCBOutfeed_PlaceOK6_SpeedX, model.PCBOutfeed_PlaceOK6_SpeedY,
                model.PCBOutfeed_PlaceOK6_Accel, model.PCBOutfeed_PlaceOK6_Decel);

            var ngPlace = OutfeedPositions.FirstOrDefault(p => p.Position == "NG Place");
            if (ngPlace != null)
            {
                ngPlace.X = model.PCBOutfeed_PlaceNGX;
                ngPlace.Y = model.PCBOutfeed_PlaceNGY;
                ngPlace.SpeedX = model.PCBOutfeed_PlaceNG_SpeedX;
                ngPlace.SpeedY = model.PCBOutfeed_PlaceNG_SpeedY;
                ngPlace.Accel = model.PCBOutfeed_PlaceNG_Accel;
                ngPlace.Decel = model.PCBOutfeed_PlaceNG_Decel;
            }

            var pickupTray = OutfeedPositions.FirstOrDefault(p => p.Position == "Pickup Tray");
            if (pickupTray != null)
            {
                pickupTray.X = model.PCBOutfeed_PickupTrayX;
                pickupTray.Y = model.PCBOutfeed_PickupTrayY;
                pickupTray.SpeedX = model.PCBOutfeed_PickupTray_SpeedX;
                pickupTray.SpeedY = model.PCBOutfeed_PickupTray_SpeedY;
                pickupTray.Accel = model.PCBOutfeed_PickupTray_Accel;
                pickupTray.Decel = model.PCBOutfeed_PickupTray_Decel;
            }

            var placeTray = OutfeedPositions.FirstOrDefault(p => p.Position == "Place Tray");
            if (placeTray != null)
            {
                placeTray.X = model.PCBOutfeed_PlaceTrayX;
                placeTray.Y = model.PCBOutfeed_PlaceTrayY;
                placeTray.SpeedX = model.PCBOutfeed_PlaceTray_SpeedX;
                placeTray.SpeedY = model.PCBOutfeed_PlaceTray_SpeedY;
                placeTray.Accel = model.PCBOutfeed_PlaceTray_Accel;
                placeTray.Decel = model.PCBOutfeed_PlaceTray_Decel;
            }
        }

        private void LoadOutfeedOKPlacePosition(PCBModel model, string positionName,
            float x, float y, float speedX, float speedY, float accel, float decel)
        {
            var position = OutfeedPositions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                position.X = x;
                position.Y = y;
                position.SpeedX = speedX;
                position.SpeedY = speedY;
                position.Accel = accel;
                position.Decel = decel;
            }
        }

        public void LoadInspect1Positions(PCBModel model)
        {
            var idle = Inspect1Positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.Z = model.Inspect1_IdleZ;
                idle.C = model.Inspect1_IdleR;
                idle.SpeedZ = model.Inspect1_Idle_SpeedZ;
                idle.Speed = model.Inspect1_Idle_SpeedC;
                idle.Accel = model.Inspect1_Idle_Accel;
                idle.Decel = model.Inspect1_Idle_Decel;
            }

            var focus1 = Inspect1Positions.FirstOrDefault(p => p.Position == "Focus 1");
            if (focus1 != null)
            {
                focus1.Z = model.Inspect1_Focus1;
                focus1.C = model.Inspect1_Rotate1;
                focus1.SpeedZ = model.Inspect1_Focus1_SpeedZ;
                focus1.Speed = model.Inspect1_Focus1_SpeedC;
                focus1.Accel = model.Inspect1_Focus1_Accel;
                focus1.Decel = model.Inspect1_Focus1_Decel;
            }

            var focus2 = Inspect1Positions.FirstOrDefault(p => p.Position == "Focus 2");
            if (focus2 != null)
            {
                focus2.Z = model.Inspect1_Focus2;
                focus2.C = model.Inspect1_Rotate2;
                focus2.SpeedZ = model.Inspect1_Focus2_SpeedZ;
                focus2.Speed = model.Inspect1_Focus2_SpeedC;
                focus2.Accel = model.Inspect1_Focus2_Accel;
                focus2.Decel = model.Inspect1_Focus2_Decel;
            }

            var focus3 = Inspect1Positions.FirstOrDefault(p => p.Position == "Focus 3");
            if (focus3 != null)
            {
                focus3.Z = model.Inspect1_Focus3;
                focus3.C = model.Inspect1_Rotate3;
                focus3.SpeedZ = model.Inspect1_Focus3_SpeedZ;
                focus3.Speed = model.Inspect1_Focus3_SpeedC;
                focus3.Accel = model.Inspect1_Focus3_Accel;
                focus3.Decel = model.Inspect1_Focus3_Decel;
            }
        }

        public void LoadInspect2Positions(PCBModel model)
        {
            var idle = Inspect2Positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                idle.Z = model.Inspect2_IdleZ;
                idle.C = model.Inspect2_IdleR;
                idle.SpeedZ = model.Inspect2_Idle_SpeedZ;
                idle.Speed = model.Inspect2_Idle_SpeedC;
                idle.Accel = model.Inspect2_Idle_Accel;
                idle.Decel = model.Inspect2_Idle_Decel;
            }

            var focus1 = Inspect2Positions.FirstOrDefault(p => p.Position == "Focus 1");
            if (focus1 != null)
            {
                focus1.Z = model.Inspect2_Focus1;
                focus1.C = model.Inspect2_Rotate1;
                focus1.SpeedZ = model.Inspect2_Focus1_SpeedZ;
                focus1.Speed = model.Inspect2_Focus1_SpeedC;
                focus1.Accel = model.Inspect2_Focus1_Accel;
                focus1.Decel = model.Inspect2_Focus1_Decel;
            }

            var focus2 = Inspect2Positions.FirstOrDefault(p => p.Position == "Focus 2");
            if (focus2 != null)
            {
                focus2.Z = model.Inspect2_Focus2;
                focus2.C = model.Inspect2_Rotate2;
                focus2.SpeedZ = model.Inspect2_Focus2_SpeedZ;
                focus2.Speed = model.Inspect2_Focus2_SpeedC;
                focus2.Accel = model.Inspect2_Focus2_Accel;
                focus2.Decel = model.Inspect2_Focus2_Decel;
            }

            var focus3 = Inspect2Positions.FirstOrDefault(p => p.Position == "Focus 3");
            if (focus3 != null)
            {
                focus3.Z = model.Inspect2_Focus3;
                focus3.C = model.Inspect2_Rotate3;
                focus3.SpeedZ = model.Inspect2_Focus3_SpeedZ;
                focus3.Speed = model.Inspect2_Focus3_SpeedC;
                focus3.Accel = model.Inspect2_Focus3_Accel;
                focus3.Decel = model.Inspect2_Focus3_Decel;
            }

            var unload = Inspect2Positions.FirstOrDefault(p => p.Position == "Unload");
            if (unload != null)
            {
                unload.Z = model.Inspect2_UnloadZ;
                unload.C = model.Inspect2_UnloadC;
                unload.SpeedZ = model.Inspect2_Unload_SpeedZ;
                unload.Speed = model.Inspect2_Unload_SpeedC;
                unload.Accel = model.Inspect2_Unload_Accel;
                unload.Decel = model.Inspect2_Unload_Decel;
            }
        }

        #endregion

        #region Save Operations

        public void SaveInfeedPositions(PCBModel model)
        {
            var idle = InfeedPositions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                model.PCBInfeed_IdleX = idle.X;
                model.PCBInfeed_IdleY = idle.Y;
                model.PCBInfeed_IdleR = idle.R;
                model.PCBInfeed_Idle_SpeedX = idle.SpeedX;
                model.PCBInfeed_Idle_SpeedY = idle.SpeedY;
                model.PCBInfeed_Idle_SpeedR = idle.SpeedR;
                model.PCBInfeed_Idle_Accel = idle.Accel;
                model.PCBInfeed_Idle_Decel = idle.Decel;
            }

            var pickup = InfeedPositions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                model.PCBInfeed_PickupX = pickup.X;
                model.PCBInfeed_PickupY = pickup.Y;
                model.PCBInfeed_PickupR = pickup.R;
                model.PCBInfeed_Pickup_SpeedX = pickup.SpeedX;
                model.PCBInfeed_Pickup_SpeedY = pickup.SpeedY;
                model.PCBInfeed_Pickup_SpeedR = pickup.SpeedR;
                model.PCBInfeed_Pickup_Accel = pickup.Accel;
                model.PCBInfeed_Pickup_Decel = pickup.Decel;
            }

            var place = InfeedPositions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                model.PCBInfeed_PlaceX = place.X;
                model.PCBInfeed_PlaceY = place.Y;
                model.PCBInfeed_PlaceR = place.R;
                model.PCBInfeed_Place_SpeedX = place.SpeedX;
                model.PCBInfeed_Place_SpeedY = place.SpeedY;
                model.PCBInfeed_Place_SpeedR = place.SpeedR;
                model.PCBInfeed_Place_Accel = place.Accel;
                model.PCBInfeed_Place_Decel = place.Decel;
            }
        }

        public void SaveTransferPositions(PCBModel model)
        {
            var idle = TransferPositions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                model.PCBTransfer_IdleX = idle.X;
                model.PCBTransfer_IdleZ = idle.Z;
                model.PCBTransfer_Idle_SpeedX = idle.SpeedX;
                model.PCBTransfer_Idle_SpeedZ = idle.SpeedZ;
                model.PCBTransfer_Idle_Accel = idle.Accel;
                model.PCBTransfer_Idle_Decel = idle.Decel;
            }

            var preparePickup = TransferPositions.FirstOrDefault(p => p.Position == "Prepare Pickup");
            if (preparePickup != null)
            {
                model.PCBTransfer_PreparePickupX = preparePickup.X;
                model.PCBTransfer_PreparePickupZ = preparePickup.Z;
                model.PCBTransfer_PreparePickup_SpeedX = preparePickup.SpeedX;
                model.PCBTransfer_PreparePickup_SpeedZ = preparePickup.SpeedZ;
                model.PCBTransfer_PreparePickup_Accel = preparePickup.Accel;
                model.PCBTransfer_PreparePickup_Decel = preparePickup.Decel;
            }

            var pickup = TransferPositions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                model.PCBTransfer_PickupX = pickup.X;
                model.PCBTransfer_PickupZ = pickup.Z;
                model.PCBTransfer_Pickup_SpeedX = pickup.SpeedX;
                model.PCBTransfer_Pickup_SpeedZ = pickup.SpeedZ;
                model.PCBTransfer_Pickup_Accel = pickup.Accel;
                model.PCBTransfer_Pickup_Decel = pickup.Decel;
            }

            var preparePlace = TransferPositions.FirstOrDefault(p => p.Position == "Prepare Place");
            if (preparePlace != null)
            {
                model.PCBTransfer_PreparePlaceX = preparePlace.X;
                model.PCBTransfer_PreparePlaceZ = preparePlace.Z;
                model.PCBTransfer_PreparePlace_SpeedX = preparePlace.SpeedX;
                model.PCBTransfer_PreparePlace_SpeedZ = preparePlace.SpeedZ;
                model.PCBTransfer_PreparePlace_Accel = preparePlace.Accel;
                model.PCBTransfer_PreparePlace_Decel = preparePlace.Decel;
            }

            var place = TransferPositions.FirstOrDefault(p => p.Position == "Place");
            if (place != null)
            {
                model.PCBTransfer_PlaceX = place.X;
                model.PCBTransfer_PlaceZ = place.Z;
                model.PCBTransfer_Place_SpeedX = place.SpeedX;
                model.PCBTransfer_Place_SpeedZ = place.SpeedZ;
                model.PCBTransfer_Place_Accel = place.Accel;
                model.PCBTransfer_Place_Decel = place.Decel;
            }

            var ng = TransferPositions.FirstOrDefault(p => p.Position == "NG Position");
            if (ng != null)
            {
                model.PCBTransfer_NGX = ng.X;
                model.PCBTransfer_NGZ = ng.Z;
                model.PCBTransfer_NG_SpeedX = ng.SpeedX;
                model.PCBTransfer_NG_SpeedZ = ng.SpeedZ;
                model.PCBTransfer_NG_Accel = ng.Accel;
                model.PCBTransfer_NG_Decel = ng.Decel;
            }
        }

        public void SaveOutfeedPositions(PCBModel model)
        {
            var idle = OutfeedPositions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                model.PCBOutfeed_IdleX = idle.X;
                model.PCBOutfeed_IdleY = idle.Y;
                model.PCBOutfeed_Idle_SpeedX = idle.SpeedX;
                model.PCBOutfeed_Idle_SpeedY = idle.SpeedY;
                model.PCBOutfeed_Idle_Accel = idle.Accel;
                model.PCBOutfeed_Idle_Decel = idle.Decel;
            }

            var pickup = OutfeedPositions.FirstOrDefault(p => p.Position == "Pickup");
            if (pickup != null)
            {
                model.PCBOutfeed_PickupX = pickup.X;
                model.PCBOutfeed_PickupY = pickup.Y;
                model.PCBOutfeed_Pickup_SpeedX = pickup.SpeedX;
                model.PCBOutfeed_Pickup_SpeedY = pickup.SpeedY;
                model.PCBOutfeed_Pickup_Accel = pickup.Accel;
                model.PCBOutfeed_Pickup_Decel = pickup.Decel;
            }

            // OK Place positions
            SaveOutfeedOKPlacePosition(model, "OK Place 1",
                ref model.PCBOutfeed_PlaceOK1X, ref model.PCBOutfeed_PlaceOK1Y,
                ref model.PCBOutfeed_PlaceOK1_SpeedX, ref model.PCBOutfeed_PlaceOK1_SpeedY,
                ref model.PCBOutfeed_PlaceOK1_Accel, ref model.PCBOutfeed_PlaceOK1_Decel);

            SaveOutfeedOKPlacePosition(model, "OK Place 2",
                ref model.PCBOutfeed_PlaceOK2X, ref model.PCBOutfeed_PlaceOK2Y,
                ref model.PCBOutfeed_PlaceOK2_SpeedX, ref model.PCBOutfeed_PlaceOK2_SpeedY,
                ref model.PCBOutfeed_PlaceOK2_Accel, ref model.PCBOutfeed_PlaceOK2_Decel);

            SaveOutfeedOKPlacePosition(model, "OK Place 3",
                ref model.PCBOutfeed_PlaceOK3X, ref model.PCBOutfeed_PlaceOK3Y,
                ref model.PCBOutfeed_PlaceOK3_SpeedX, ref model.PCBOutfeed_PlaceOK3_SpeedY,
                ref model.PCBOutfeed_PlaceOK3_Accel, ref model.PCBOutfeed_PlaceOK3_Decel);

            SaveOutfeedOKPlacePosition(model, "OK Place 4",
                ref model.PCBOutfeed_PlaceOK4X, ref model.PCBOutfeed_PlaceOK4Y,
                ref model.PCBOutfeed_PlaceOK4_SpeedX, ref model.PCBOutfeed_PlaceOK4_SpeedY,
                ref model.PCBOutfeed_PlaceOK4_Accel, ref model.PCBOutfeed_PlaceOK4_Decel);

            SaveOutfeedOKPlacePosition(model, "OK Place 5",
                ref model.PCBOutfeed_PlaceOK5X, ref model.PCBOutfeed_PlaceOK5Y,
                ref model.PCBOutfeed_PlaceOK5_SpeedX, ref model.PCBOutfeed_PlaceOK5_SpeedY,
                ref model.PCBOutfeed_PlaceOK5_Accel, ref model.PCBOutfeed_PlaceOK5_Decel);

            SaveOutfeedOKPlacePosition(model, "OK Place 6",
                ref model.PCBOutfeed_PlaceOK6X, ref model.PCBOutfeed_PlaceOK6Y,
                ref model.PCBOutfeed_PlaceOK6_SpeedX, ref model.PCBOutfeed_PlaceOK6_SpeedY,
                ref model.PCBOutfeed_PlaceOK6_Accel, ref model.PCBOutfeed_PlaceOK6_Decel);

            var ngPlace = OutfeedPositions.FirstOrDefault(p => p.Position == "NG Place");
            if (ngPlace != null)
            {
                model.PCBOutfeed_PlaceNGX = ngPlace.X;
                model.PCBOutfeed_PlaceNGY = ngPlace.Y;
                model.PCBOutfeed_PlaceNG_SpeedX = ngPlace.SpeedX;
                model.PCBOutfeed_PlaceNG_SpeedY = ngPlace.SpeedY;
                model.PCBOutfeed_PlaceNG_Accel = ngPlace.Accel;
                model.PCBOutfeed_PlaceNG_Decel = ngPlace.Decel;
            }

            var pickupTray = OutfeedPositions.FirstOrDefault(p => p.Position == "Pickup Tray");
            if (pickupTray != null)
            {
                model.PCBOutfeed_PickupTrayX = pickupTray.X;
                model.PCBOutfeed_PickupTrayY = pickupTray.Y;
                model.PCBOutfeed_PickupTray_SpeedX = pickupTray.SpeedX;
                model.PCBOutfeed_PickupTray_SpeedY = pickupTray.SpeedY;
                model.PCBOutfeed_PickupTray_Accel = pickupTray.Accel;
                model.PCBOutfeed_PickupTray_Decel = pickupTray.Decel;
            }

            var placeTray = OutfeedPositions.FirstOrDefault(p => p.Position == "Place Tray");
            if (placeTray != null)
            {
                model.PCBOutfeed_PlaceTrayX = placeTray.X;
                model.PCBOutfeed_PlaceTrayY = placeTray.Y;
                model.PCBOutfeed_PlaceTray_SpeedX = placeTray.SpeedX;
                model.PCBOutfeed_PlaceTray_SpeedY = placeTray.SpeedY;
                model.PCBOutfeed_PlaceTray_Accel = placeTray.Accel;
                model.PCBOutfeed_PlaceTray_Decel = placeTray.Decel;
            }
        }

        private void SaveOutfeedOKPlacePosition(PCBModel model, string positionName,
            ref float x, ref float y, ref float speedX, ref float speedY, ref float accel, ref float decel)
        {
            var position = OutfeedPositions.FirstOrDefault(p => p.Position == positionName);
            if (position != null)
            {
                x = position.X;
                y = position.Y;
                speedX = position.SpeedX;
                speedY = position.SpeedY;
                accel = position.Accel;
                decel = position.Decel;
            }
        }

        public void SaveInspect1Positions(PCBModel model)
        {
            var idle = Inspect1Positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                model.Inspect1_IdleZ = idle.Z;
                model.Inspect1_IdleR = idle.C;
                model.Inspect1_Idle_SpeedZ = idle.SpeedZ;
                model.Inspect1_Idle_SpeedC = idle.Speed;
                model.Inspect1_Idle_Accel = idle.Accel;
                model.Inspect1_Idle_Decel = idle.Decel;
            }

            var focus1 = Inspect1Positions.FirstOrDefault(p => p.Position == "Focus 1");
            if (focus1 != null)
            {
                model.Inspect1_Focus1 = focus1.Z;
                model.Inspect1_Rotate1 = focus1.C;
                model.Inspect1_Focus1_SpeedZ = focus1.SpeedZ;
                model.Inspect1_Focus1_SpeedC = focus1.Speed;
                model.Inspect1_Focus1_Accel = focus1.Accel;
                model.Inspect1_Focus1_Decel = focus1.Decel;
            }

            var focus2 = Inspect1Positions.FirstOrDefault(p => p.Position == "Focus 2");
            if (focus2 != null)
            {
                model.Inspect1_Focus2 = focus2.Z;
                model.Inspect1_Rotate2 = focus2.C;
                model.Inspect1_Focus2_SpeedZ = focus2.SpeedZ;
                model.Inspect1_Focus2_SpeedC = focus2.Speed;
                model.Inspect1_Focus2_Accel = focus2.Accel;
                model.Inspect1_Focus2_Decel = focus2.Decel;
            }

            var focus3 = Inspect1Positions.FirstOrDefault(p => p.Position == "Focus 3");
            if (focus3 != null)
            {
                model.Inspect1_Focus3 = focus3.Z;
                model.Inspect1_Rotate3 = focus3.C;
                model.Inspect1_Focus3_SpeedZ = focus3.SpeedZ;
                model.Inspect1_Focus3_SpeedC = focus3.Speed;
                model.Inspect1_Focus3_Accel = focus3.Accel;
                model.Inspect1_Focus3_Decel = focus3.Decel;
            }
        }

        public void SaveInspect2Positions(PCBModel model)
        {
            var idle = Inspect2Positions.FirstOrDefault(p => p.Position == "Idle");
            if (idle != null)
            {
                model.Inspect2_IdleZ = idle.Z;
                model.Inspect2_IdleR = idle.C;
                model.Inspect2_Idle_SpeedZ = idle.SpeedZ;
                model.Inspect2_Idle_SpeedC = idle.Speed;
                model.Inspect2_Idle_Accel = idle.Accel;
                model.Inspect2_Idle_Decel = idle.Decel;
            }

            var focus1 = Inspect2Positions.FirstOrDefault(p => p.Position == "Focus 1");
            if (focus1 != null)
            {
                model.Inspect2_Focus1 = focus1.Z;
                model.Inspect2_Rotate1 = focus1.C;
                model.Inspect2_Focus1_SpeedZ = focus1.SpeedZ;
                model.Inspect2_Focus1_SpeedC = focus1.Speed;
                model.Inspect2_Focus1_Accel = focus1.Accel;
                model.Inspect2_Focus1_Decel = focus1.Decel;
            }

            var focus2 = Inspect2Positions.FirstOrDefault(p => p.Position == "Focus 2");
            if (focus2 != null)
            {
                model.Inspect2_Focus2 = focus2.Z;
                model.Inspect2_Rotate2 = focus2.C;
                model.Inspect2_Focus2_SpeedZ = focus2.SpeedZ;
                model.Inspect2_Focus2_SpeedC = focus2.Speed;
                model.Inspect2_Focus2_Accel = focus2.Accel;
                model.Inspect2_Focus2_Decel = focus2.Decel;
            }

            var focus3 = Inspect2Positions.FirstOrDefault(p => p.Position == "Focus 3");
            if (focus3 != null)
            {
                model.Inspect2_Focus3 = focus3.Z;
                model.Inspect2_Rotate3 = focus3.C;
                model.Inspect2_Focus3_SpeedZ = focus3.SpeedZ;
                model.Inspect2_Focus3_SpeedC = focus3.Speed;
                model.Inspect2_Focus3_Accel = focus3.Accel;
                model.Inspect2_Focus3_Decel = focus3.Decel;
            }

            var unload = Inspect2Positions.FirstOrDefault(p => p.Position == "Unload");
            if (unload != null)
            {
                model.Inspect2_UnloadZ = unload.Z;
                model.Inspect2_UnloadC = unload.C;
                model.Inspect2_Unload_SpeedZ = unload.SpeedZ;
                model.Inspect2_Unload_SpeedC = unload.Speed;
                model.Inspect2_Unload_Accel = unload.Accel;
                model.Inspect2_Unload_Decel = unload.Decel;
            }
        }

        #endregion

        #region PLC Position Sets (Used by ModelConfig.RobotPositions for direct PLC read/write)

        /// <summary>
        /// Shortcut position sets for PLC communication
        /// </summary>
        public RobotPositionSet Infeed { get; } = new RobotPositionSet();
        public RobotPositionSet Transfer { get; } = new RobotPositionSet();
        public RobotPositionSet Outfeed { get; } = new RobotPositionSet();
        public RobotPositionSet Inspect1 { get; } = new RobotPositionSet();
        public RobotPositionSet Inspect2 { get; } = new RobotPositionSet();

        /// <summary>
        /// Write all position sets to PLC
        /// </summary>
        public async System.Threading.Tasks.Task SaveAllPositionsToPLCAsync(IPlcService plc)
        {
            if (plc == null || !plc.IsConnected) return;

            await System.Threading.Tasks.Task.Run(() =>
            {
                // Write Infeed positions
                WritePositionSetToPLC(plc, "Infeed", Infeed);
                // Write Transfer positions
                WritePositionSetToPLC(plc, "Transfer", Transfer);
                // Write Outfeed positions
                WritePositionSetToPLC(plc, "Outfeed", Outfeed);
                // Write Inspect1 positions
                WritePositionSetToPLC(plc, "Inspect1", Inspect1);
                // Write Inspect2 positions
                WritePositionSetToPLC(plc, "Inspect2", Inspect2);
            });
        }

        private void WritePositionSetToPLC(IPlcService plc, string prefix, RobotPositionSet pos)
        {
            plc.WriteDouble($"{prefix}_StandbyX", pos.StandbyX);
            plc.WriteDouble($"{prefix}_StandbyY", pos.StandbyY);
            plc.WriteDouble($"{prefix}_StandbyZ", pos.StandbyZ);
            plc.WriteDouble($"{prefix}_PickX", pos.PickX);
            plc.WriteDouble($"{prefix}_PickY", pos.PickY);
            plc.WriteDouble($"{prefix}_PickZ", pos.PickZ);
            plc.WriteDouble($"{prefix}_PlaceX", pos.PlaceX);
            plc.WriteDouble($"{prefix}_PlaceY", pos.PlaceY);
            plc.WriteDouble($"{prefix}_PlaceZ", pos.PlaceZ);
        }

        #endregion
    }

    /// <summary>
    /// DTO for robot position data used in PLC communication
    /// </summary>
    public class RobotPositionSet
    {
        public double StandbyX { get; set; }
        public double StandbyY { get; set; }
        public double StandbyZ { get; set; }
        public double PickX { get; set; }
        public double PickY { get; set; }
        public double PickZ { get; set; }
        public double PlaceX { get; set; }
        public double PlaceY { get; set; }
        public double PlaceZ { get; set; }
    }
}



