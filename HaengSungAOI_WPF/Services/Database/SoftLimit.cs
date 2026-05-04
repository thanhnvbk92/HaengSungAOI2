using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HaengSungAOI_WPF.Services.Database
{
    internal class SoftLimit
    {
        public const double encoder_pulse_per_rotation = 8388608;
        public const double um_per_rotation = 10000;
        public const double mm_per_rotation = 10;
        //public const double pulse_per_um = 838.8608;
        //public const double pulse_per_um_X1 = 838.8608;
        public const double pulse_per_um_X1 = 419.4304;
        //public const double pulse_per_um_Y1 = 838.8608;
        public const double pulse_per_um_Y1 = 419.4304;
        //public const double pulse_per_um_Z3 = 838.8608;
        public const double pulse_per_um_Z3 = 419.4304;
        //public const double pulse_per_um_Z4 = 838.8608;
        public const double pulse_per_um_Z4 = 419.4304;
        public const double pulse_per_um_Z5 = 838.8608;
        public const double pulse_per_um_Z6 = 838.8608;
        //public const double pulse_per_um_Z2 = 838.8608;
        public const double pulse_per_um_Z2 = 419.4304;
        //public const double pulse_per_um_Y3 = 838.8608;
        public const double pulse_per_um_Y3 = 419.4304;
        public const double pulse_per_um_X3 = 419.4304;
        public const double pulse_per_degree = 23301.689; // 8388608 pulses per rotation, 360 degrees in a rotation
        //public const double pulse_per_degree_C3 = 34952.5335;
        public const double pulse_per_degree_C3 = 34181.9;
        public const double pulse_per_unit_NGConv = 1000000; // 10000 pulses per 1 unit of NG conveyor movement

        // Soft limits for the Infeed robot axes
        public const double axis_X1_min = 0;
        public const double axis_X1_max = 380000;
        public const double axis_Y1_min = 0;
        public const double axis_Y1_max = 300000;

        // Soft limits for the Transfer robot axes
        public const double axis_X2_min = -10000;
        public const double axis_X2_max = 395000;
        public const double axis_Z2_min = 0;
        public const double axis_Z2_max = 137000;
        // Soft limits for the Outfeed robot axes

        public const double axis_X3_min = 0;
        public const double axis_X3_max = 663000;
        public const double axis_Y3_min = 0;
        public const double axis_Y3_max = 675000;

        // Soft limits for the Camera 2 axes
        public const double axis_Z3_min = -10000;
        public const double axis_Z3_max = 140000;
        // Soft limits for the Camera 3 axes
        public const double axis_Z4_min = 0000;
        public const double axis_Z4_max = 130000;

        // Soft limits for the PCB Rotate 1 axes
        public const double axis_C2_min = -180;
        public const double axis_C2_max = 180;

        //Soft limits for the PCB Rotate 2 axes
        public const double axis_C3_min = 0;
        public const double axis_C3_max = 410;

        //Soft limits for the Z5 and Z6 axes
        public const double axis_Z5_min = 0;
        public const double axis_Z5_max = 575000;
        public const double axis_Z6_min = 0;
        public const double axis_Z6_max = 600000;

        //Default Robot 1 Place Position
        public const double axis_X1_place = 315000;
        public const double axis_Y1_place = 190000;
        public const double axis_C1_place = 0;

        //Default Robot 1 Pick Position
        public const double axis_X1_pick = 103893;
        public const double axis_Y1_pick = 202018;

        //Default Robot 2 NG Position
        public const double axis_X2_ng = 200000;      
        public const double axis_Z2_ng = 135000;

        //Default Robot 2 Pick Position
        public const double axis_X2_pick = 395000;      
        public const double axis_Z2_pick = 28000;

        //Default Robot 2 Place Position
        public const double axis_X2_place = 2000;
        public const double axis_Z2_place = 44000;

        //Default Robot 2 Exit position
        public const double axis_X2_exit = 185000;
        public const double axis_Z2_exit = 0;

        //Default Camera 1 Focus Position
        public const double axis_Z3_focus_1 = 140315;
        public const double axis_Z3_focus_2 = 94000;
        public const double axis_Z3_focus_3 = 94000;

        //Default C2 PCB Rotate Position
        public const double axis_C2_rotate_1 = 0;
        public const double axis_C2_rotate_2 = 90;
        public const double axis_C2_rotate_3 = -86;

        //Default Camera 2 Focus Position
        public const double axis_Z4_focus_1 = 124000;
        public const double axis_Z4_focus_2 = 94500;
        public const double axis_Z4_focus_3 = 106154;

        //Default C3 PCB Rotate Position
        //public const double axis_C3_rotate_1 = 169;
        //public const double axis_C3_rotate_2 = -6;
        //public const double axis_C3_rotate_3 = -95;
        public const double axis_C3_rotate_1 = 173;
        public const double axis_C3_rotate_2 = 263;
        public const double axis_C3_rotate_3 = 353;

        //Default Outfeed Robot 3 Pick Position
        public const double axis_X3_pick = 675000;
        public const double axis_Y3_pick = 135000;

        //Default Outfeed Robot 3 NG Position
        public const double axis_X3_ng = 645000;
        public const double axis_Y3_ng = 200000;

        //Robot 3 Paletizing position 1
        public const double axis_Y3_OK_1 = 38500;
        public const double axis_Y3_OK_2 = 163500;
        public const double axis_Y3_OK_3 = 288500;

        public const double axis_Y3_OK_4 = 38500;
        public const double axis_Y3_OK_5 = 163500;
        public const double axis_Y3_OK_6 = 288500;

        public const double axis_X3_OK_1 = 24000;
        public const double axis_X3_OK_2 = 23500;
        public const double axis_X3_OK_3 = 22000;

        public const double axis_X3_OK_4 = 206000;
        public const double axis_X3_OK_5 = 205500;
        public const double axis_X3_OK_6 = 205000;






    }
}



