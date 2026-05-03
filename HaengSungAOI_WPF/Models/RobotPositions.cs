using System;

namespace HaengSungAOI_WPF.Models
{
    public class RobotPositions
    {
        // Transfer Robot positions
        public float IdleX { get; set; }
        public float IdleY { get; set; }
        public float IdleZ { get; set; }
        public float IdleR { get; set; }
        
        public float PreparePickupX { get; set; }
        public float PreparePickupY { get; set; }
        public float PreparePickupZ { get; set; }
        public float PreparePickupR { get; set; }
        
        public float PickupX { get; set; }
        public float PickupY { get; set; }
        public float PickupZ { get; set; }
        public float PickupR { get; set; }
        
        public float PreparePlaceX { get; set; }
        public float PreparePlaceY { get; set; }
        public float PreparePlaceZ { get; set; }
        public float PreparePlaceR { get; set; }
        
        public float PlaceX { get; set; }
        public float PlaceY { get; set; }
        public float PlaceZ { get; set; }
        public float PlaceR { get; set; }
        
        // NG position for rejected PCBs
        public float NGX { get; set; }
        public float NGY { get; set; }
        public float NGZ { get; set; }
        
        // Camera focus positions
        public float Focus1 { get; set; }
        public float Focus2 { get; set; }
        public float Focus3 { get; set; }
        
        // Rotation positions
        public float Rotate1 { get; set; }
        public float Rotate2 { get; set; }
        public float Rotate3 { get; set; }
        
        // Movement parameters
        public float Speed { get; set; }
        public float AccTime { get; set; }
        public float DecTime { get; set; }
    }
}