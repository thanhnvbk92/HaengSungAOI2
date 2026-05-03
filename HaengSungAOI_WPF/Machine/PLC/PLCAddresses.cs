using System.Collections.Generic;
using HaengSungAOI_WPF.Machine.PLC;

namespace HaengSungAOI_WPF.Machine.PLC.PLC
{
    public static class PLCAddresses
    {
        public static readonly Dictionary<string, ushort> HMI_PushButtons = new Dictionary<string, ushort>
        {
            { "HMI_Auto_PB", 0 },
            { "HMI_Manual_PB", 1 },
            { "HMI_Reset_PB", 2 },
            { "HMI_Origin", 3 },
            { "HMI_Start", 4 },
            { "HMI_Stop", 5 },
            { "HMI_Pause_System", 6 },
            { "HMI_Single_Block_Mode", 7 },
            { "HMI_Next_Step_PB", 8 },
            { "HMI_Buzzer_Off", 9 },
            { "HMI_End_Cycle", 10 },
            { "HMI_Counter_Reset_PB", 11 },
            { "HMI_Tray_0030_No1", 12 },
            { "HMI_Tray_0030_No2", 13 },
            { "HMI_Cyl_Infeed_Up_PB", 32 },
            { "HMI_Cyl_Infeed_Down_PB", 33 },
            { "HMI_Vacuum_Infeed_ON_PB", 34 },
            { "HMI_Vacuum_Infeed_OFF_PB", 35 },
            { "HMI_Cyl_NG_Up_PB", 36 },
            { "HMI_Cyl_NG_Down_PB", 37 },
            { "HMI_Vacuum_Transfer_ON_PB", 38 },
            { "HMI_Vacuum_Transfer_OFF_PB", 39 },
            { "HMI_Cyl_Outfeed_Up_PB", 40 },
            { "HMI_Cyl_Outfeed_Down_PB", 41 },
            { "HMI_Vacuum_Outfeed_ON_PB", 42 },
            { "HMI_Vacuum_Outfeed_OFF_PB", 43 },
            { "HMI_Cyl_Pickup_Tray_Up_PB", 44 },
            { "HMI_Cyl_Pickup_Tray_Down_PB", 45 },
            { "HMI_Vacuum_Pickup_Tray_ON_PB", 46 },
            { "HMI_Vacuum_Pickup_Tray_OFF_PB", 47 },
            { "HMI_Vacuum_Inspect1_ON_PB", 48 },
            { "HMI_Vacuum_Inspect1_OFF_PB", 49 },
            { "HMI_Vacuum_Inspect2_ON_PB", 50 },
            { "HMI_Vacuum_Inspect2_OFF_PB", 51 },
            { "HMI_NG_CV_ON_PB", 52 },
            { "HMI_NG_CV_OFF_PB", 53 },
            { "HMI_Camera_1_Trigger_ON_PB", 54 },
            { "HMI_Camera_1_Trigger_OFF_PB", 55 },
            { "HMI_Camera_2_Trigger_ON_PB", 56 },
            { "HMI_Camera_2_Trigger_OFF_PB", 57 },
            { "HMI_Camera_3_Trigger_ON_PB", 58 },
            { "HMI_Camera_3_Trigger_OFF_PB", 59 },

            { "HMI_PCB_Infeed_Move_to_Idle_PB", 80 },
            { "HMI_PCB_Infeed_Move_to_Pickup_PB", 81 },
            { "HMI_PCB_Infeed_Move_to_Place_PB", 82 },

            { "HMI_PCB_Transfer_Move_to_Idle_PB", 90 },
            { "HMI_PCB_Transfer_Move_to_Pickup_PB", 91 },
            { "HMI_PCB_Transfer_Move_to_Prepare_Pickup_PB", 92 },
            { "HMI_PCB_Transfer_Move_to_Prepare_Place_PB", 93 },
            { "HMI_PCB_Transfer_Move_to_Place_PB", 94 },
            { "HMI_PCB_Transfer_Move_to_NG_PB", 95 },

            { "HMI_PCB_Outfeed_Move_to_Idle_PB", 100 },
            { "HMI_PCB_Outfeed_Move_to_Pickup_PB", 101 },
            { "HMI_PCB_Outfeed_Move_to_OK1_PB", 102 },
            { "HMI_PCB_Outfeed_Move_to_OK2_PB", 103 },
            { "HMI_PCB_Outfeed_Move_to_OK3_PB", 104 },
            { "HMI_PCB_Outfeed_Move_to_OK4_PB", 105 },
            { "HMI_PCB_Outfeed_Move_to_OK5_PB", 106 },
            { "HMI_PCB_Outfeed_Move_to_OK6_PB", 107 },
            { "HMI_PCB_Outfeed_Move_to_NG_PB", 108 },
            { "HMI_PCB_Outfeed_Move_to_Tray_Pickup_PB", 109 },
            { "HMI_PCB_Outfeed_Move_to_Tray_Place_PB", 110 },
            { "HMI_PCB_Outfeed_Move_to_Home_X_Axis_PB", 111 },
            { "HMI_PCB_Outfeed_New_Tray_PB", 112 },

            { "HMI_PCB_Inspect_1_Move_to_Idle_PB", 120 },
            { "HMI_PCB_Inspect_1_Focus_Position_1_PB", 121 },
            { "HMI_PCB_Inspect_1_Focus_Position_2_PB", 122 },
            { "HMI_PCB_Inspect_1_Focus_Position_3_PB", 123 },
            { "HMI_PCB_Inspect_1_Start_Inspect_PB", 124 },
            { "HMI_PCB_Inspect_1_Stop_Sequence_PB", 125 },

            { "HMI_PCB_Inspect_2_Move_to_Idle_PB", 130 },
            { "HMI_PCB_Inspect_2_Focus_Position_1_PB", 131 },
            { "HMI_PCB_Inspect_2_Focus_Position_2_PB", 132 },
            { "HMI_PCB_Inspect_2_Focus_Position_3_PB", 133 },
            { "HMI_PCB_Inspect_2_Start_Inspect_PB", 134 },
            { "HMI_PCB_Inspect_2_Stop_Sequence_PB", 135 },
        };

        public static readonly Dictionary<string, ushort> HMI_Lamps = new Dictionary<string, ushort>
        {
            { "HMI_Lamp_Auto_PB", 200 },
            { "HMI_Lamp_Manual_PB", 201 },
            { "HMI_Lamp_Reset_PB", 202 },
            { "HMI_Lamp_Origin", 203 },
            { "HMI_Lamp_Start", 204 },
            { "HMI_Lamp_Stop", 205 },
            { "HMI_Lamp_Pause_System", 206 },
            { "HMI_Lamp_Single_Block_Mode", 207 },
            { "HMI_Lamp_Next_Step_PB", 208 },
            { "HMI_Lamp_Buzzer_Off", 209 },
            { "HMI_Lamp_End_Cycle", 210 },
            { "HMI_Lamp_Cyl_Infeed_Up_PB", 232 },
            { "HMI_Lamp_Cyl_Infeed_Down_PB", 233 },
            { "HMI_Lamp_Vacuum_Infeed_ON_PB", 234 },
            { "HMI_Lamp_Vacuum_Infeed_OFF_PB", 235 },
            { "HMI_Lamp_Cyl_NG_Up_PB", 236 },
            { "HMI_Lamp_Cyl_NG_Down_PB", 237 },
            { "HMI_Lamp_Vacuum_Transfer_ON_PB", 238 },
            { "HMI_Lamp_Vacuum_Transfer_OFF_PB", 239 },
            { "HMI_Lamp_Cyl_Outfeed_Up_PB", 240 },
            { "HMI_Lamp_Cyl_Outfeed_Down_PB", 241 },
            { "HMI_Lamp_Vacuum_Outfeed_ON_PB", 242 },
            { "HMI_Lamp_Vacuum_Outfeed_OFF_PB", 243 },
            { "HMI_Lamp_Cyl_Pickup_Tray_Up_PB", 244 },
            { "HMI_Lamp_Cyl_Pickup_Tray_Down_PB", 245 },
            { "HMI_Lamp_Vacuum_Pickup_Tray_ON_PB", 246 },
            { "HMI_Lamp_Vacuum_Pickup_Tray_OFF_PB", 247 },
            { "HMI_Lamp_Vacuum_Inspect1_ON_PB", 248 },
            { "HMI_Lamp_Vacuum_Inspect1_OFF_PB", 249 },
            { "HMI_Lamp_Vacuum_Inspect2_ON_PB", 250 },
            { "HMI_Lamp_Vacuum_Inspect2_OFF_PB", 251 },
            { "HMI_Lamp_NG_CV_ON_PB", 252 },
            { "HMI_Lamp_NG_CV_OFF_PB", 253 },
            { "HMI_Lamp_Camera_1_Trigger_ON_PB", 254 },
            { "HMI_Lamp_Camera_1_Trigger_OFF_PB", 255 },
            { "HMI_Lamp_Camera_2_Trigger_ON_PB", 256 },
            { "HMI_Lamp_Camera_2_Trigger_OFF_PB", 257 },
            { "HMI_Lamp_Camera_3_Trigger_ON_PB", 258 },
            { "HMI_Lamp_Camera_3_Trigger_OFF_PB", 259 },

            { "HMI_Lamp_PCB_Infeed_Move_to_Idle_PB", 280 },
            { "HMI_Lamp_PCB_Infeed_Move_to_Pickup_PB", 281 },
            { "HMI_Lamp_PCB_Infeed_Move_to_Place_PB", 282 },

            { "HMI_Lamp_PCB_Transfer_Move_to_Idle_PB", 290 },
            { "HMI_Lamp_PCB_Transfer_Move_to_Pickup_PB", 291 },
            { "HMI_Lamp_PCB_Transfer_Move_to_Prepare_Pickup_PB", 292 },
            { "HMI_Lamp_PCB_Transfer_Move_to_Prepare_Place_PB", 293 },
            { "HMI_Lamp_PCB_Transfer_Move_to_Place_PB", 294 },
            { "HMI_Lamp_PCB_Transfer_Move_to_NG_PB", 295 },

            { "HMI_Lamp_PCB_Outfeed_Move_to_Idle_PB", 300 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_Pickup_PB", 301 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_OK1_PB", 302 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_OK2_PB", 303 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_OK3_PB", 304 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_OK4_PB", 305 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_OK5_PB", 306 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_OK6_PB", 307 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_NG_PB", 308 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_Tray_Pickup_PB", 309 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_Tray_Place_PB", 310 },
            { "HMI_Lamp_PCB_Outfeed_Move_to_Home_X_Axis_PB", 311 },
            { "HMI_Lamp_PCB_Outfeed_New_Tray_PB", 312 },

            { "HMI_Lamp_PCB_Inspect_1_Move_to_Idle_PB", 320 },
            { "HMI_Lamp_PCB_Inspect_1_Focus_Position_1_PB", 321 },
            { "HMI_Lamp_PCB_Inspect_1_Focus_Position_2_PB", 322 },
            { "HMI_Lamp_PCB_Inspect_1_Focus_Position_3_PB", 323 },
            { "HMI_Lamp_PCB_Inspect_1_Start_Inspect_PB", 324 },
            { "HMI_Lamp_PCB_Inspect_1_Stop_Sequence_PB", 325 },

            { "HMI_Lamp_PCB_Inspect_2_Move_to_Idle_PB", 330 },
            { "HMI_Lamp_PCB_Inspect_2_Focus_Position_1_PB", 331 },
            { "HMI_Lamp_PCB_Inspect_2_Focus_Position_2_PB", 332 },
            { "HMI_Lamp_PCB_Inspect_2_Focus_Position_3_PB", 333 },
            { "HMI_Lamp_PCB_Inspect_2_Start_Inspect_PB", 334 },
            { "HMI_Lamp_PCB_Inspect_2_Stop_Sequence_PB", 335 },
        };

        public static readonly Dictionary<string, ushort> HMI_Select = new Dictionary<string, ushort>
        {
            { "HMI_Select_Fix_Jig", 0x00 },
        };

        public static readonly Dictionary<string, ushort> HMI_Select_Lamps = new Dictionary<string, ushort>
        {
            { "HMI_Lamp_Select_Fix_Jig", 0x20 },
        };

        public static readonly Dictionary<string, ushort> HMI_Select_Registers = new Dictionary<string, ushort>
        {
            { "HMI_Select_Auto", 4200 },
            { "HMI_Select_Manual", 4201 },
            { "HMI_Select_Model", 4202 },
            { "HMI_Select_IO_Monitor", 4203 },
            { "HMI_Select_Setting", 4204 },
            { "HMI_Select_Alarm", 4205 },
            { "HMI_Select_Main", 4206 },
        };

        public static readonly Dictionary<string, ushort> HMI_Select_Lamp_Registers = new Dictionary<string, ushort>
        {
            { "HMI_Lamp_Select_Auto", 4300 },
            { "HMI_Lamp_Select_Manual", 4301 },
            { "HMI_Lamp_Select_Model", 4302 },
            { "HMI_Lamp_Select_IO_Monitor", 4303 },
            { "HMI_Lamp_Select_Setting", 4304 },
            { "HMI_Lamp_Select_Alarm", 4305 },
            { "HMI_Lamp_Select_Main", 4306 },
        };

        /// <summary>
        /// Tray Quantity Registers - Track PCB slot count and tray quantities
        /// </summary>
        public static readonly Dictionary<string, ushort> TrayQuantity_Registers = new Dictionary<string, ushort>
        {
            { "PCB_Slot", 498 }, // MW498 - PCB Slot count (0-48)
            { "PCB_Trays", 499 },        // MW499 - PCB Tray quantity
            { "Blank_Trays", 4410 },       // MW4410 - Blank Tray quantity
        };

        /// <summary>
        /// Product Logging Registers - OK/NG product logging triggers and scan out results
        /// </summary>
        public static readonly Dictionary<string, ushort> ProductLog_Registers = new Dictionary<string, ushort>
        {
            // Product logging triggers
            { "Product_OK_Trigger", 481 },      // MW481 - OK product trigger (1 = log OK product)
            { "Product_NG_Trigger", 482 },      // MW482 - NG product trigger (1 = log NG product)
     
            // Slot number for scan out
            { "Product_Slot", 448 },// MW448 - Slot number for scan out  
            // Scan out result feedback
            { "ScanOut_OK", 432 },      // MW432 - Scan out OK result
            { "ScanOut_NG", 433 },           // MW433 - Scan out NG result
            { "ScanOut_NGQuantity", 434 },      // MW434 - Scan out NG Quantity result
        };

        /// <summary>
        /// OK Product Barcode Registers - 10 registers for barcode string (MW460-MW469)
        /// Each register holds 2 ASCII characters
        /// </summary>
        public static readonly Dictionary<string, ushort> OKBarcode_Registers = new Dictionary<string, ushort>
        {
            { "OK_Barcode_0", 460 },    // MW460
            { "OK_Barcode_1", 461 },    // MW461
            { "OK_Barcode_2", 462 },    // MW462
            { "OK_Barcode_3", 463 },    // MW463
            { "OK_Barcode_4", 464 },    // MW464
            { "OK_Barcode_5", 465 },    // MW465
            { "OK_Barcode_6", 466 },    // MW466
            { "OK_Barcode_7", 467 },    // MW467
            { "OK_Barcode_8", 468 },    // MW468
            { "OK_Barcode_9", 469 },    // MW469
        };

        /// <summary>
        /// NG Product Barcode Registers - 10 registers for barcode string (MW470-MW479)
        /// Each register holds 2 ASCII characters
        /// </summary>
        public static readonly Dictionary<string, ushort> NGBarcode_Registers = new Dictionary<string, ushort>
        {
            { "NG_Barcode_0", 470 },    // MW470
            { "NG_Barcode_1", 471 },    // MW471
            { "NG_Barcode_2", 472 },    // MW472
            { "NG_Barcode_3", 473 },    // MW473
            { "NG_Barcode_4", 474 }, // MW474
            { "NG_Barcode_5", 475 },    // MW475
            { "NG_Barcode_6", 476 },    // MW476
            { "NG_Barcode_7", 477 },    // MW477
            { "NG_Barcode_8", 478 },    // MW478
            { "NG_Barcode_9", 479 },    // MW479
        };

        /// <summary>
        /// Alarm/Error Registers - Machine alarm and error states from PLC
        /// Address range: MW9000 - MW9099
        /// </summary>

        public static readonly Dictionary<string, ushort> Alarm_Registers = new Dictionary<string, ushort>
      {
            // System Alarms
            { "Alarm_EMG_Stop", 9000 },                    // MW9000 - Alarm EMG Stop
            { "Alarm_Main_Pressure", 9001 },               // MW9001 - Alarm Main Pressure
            { "Alarm_Door_1_Open", 9002 },                 // MW9002 - Alarm Door 1 Open
            { "Alarm_Door_2_Open", 9003 },                 // MW9003 - Alarm Door 2 Open
            
            // Axis Alarms
            { "Alarm_X1_Axis", 9004 },                     // MW9004 - Alarm X1 Axis
            { "Alarm_Y1_Axis", 9005 },                     // MW9005 - Alarm Y1 Axis
            { "Alarm_C1_Axis", 9006 },                     // MW9006 - Alarm C1 Axis
            { "Alarm_X2_Axis", 9007 },                     // MW9007 - Alarm X2 Axis
            { "Alarm_Z2_Axis", 9008 },                     // MW9008 - Alarm Z2 Axis
            { "Alarm_X3_Axis", 9009 },                     // MW9009 - Alarm X3 Axis
            { "Alarm_Y3_Axis", 9010 },                     // MW9010 - Alarm Y3 Axis
            { "Alarm_Z4_Axis", 9011 },                     // MW9011 - Alarm Z4 Axis
            { "Alarm_C4_Axis", 9012 },                     // MW9012 - Alarm C4 Axis
            { "Alarm_Z5_Axis", 9013 },                     // MW9013 - Alarm Z5 Axis
            { "Alarm_C5_Axis", 9014 },                     // MW9014 - Alarm C5 Axis
            { "Alarm_Z61_Axis", 9015 },                    // MW9015 - Alarm Z61 Axis
            { "Alarm_Z62_Axis", 9016 },                    // MW9016 - Alarm Z62 Axis
            { "Alarm_NG_CV", 9017 },                       // MW9017 - Alarm NG CV
            
            // Cylinder Alarms
            { "Alarm_Cyl_Infeed_Up", 9018 },               // MW9018 - Alarm Cyl Infeed Up
            { "Alarm_Cyl_Infeed_Down", 9019 },             // MW9019 - Alarm Cyl Infeed Down
            { "Alarm_Cyl_NG_Up", 9020 },                   // MW9020 - Alarm Cyl NG Up
            { "Alarm_Cyl_NG_Down", 9021 },                 // MW9021 - Alarm Cyl NG Down
            { "Alarm_Cyl_Outfeed_Up", 9022 },              // MW9022 - Alarm Cyl Outfeed Up
            { "Alarm_Cyl_Outfeed_Down", 9023 },            // MW9023 - Alarm Cyl Outfeed Down
            { "Alarm_Cyl_Pickup_Tray_Up", 9024 },          // MW9024 - Alarm Cyl Pickup Tray Up
            { "Alarm_Cyl_Pickup_Tray_Down", 9025 },        // MW9025 - Alarm Cyl Pickup Tray Down
            
            // Vacuum Alarms
            { "Alarm_Vacuum_Infeed", 9026 },               // MW9026 - Alarm Vacuum Infeed
            { "Alarm_Vacuum_NG", 9027 },                   // MW9027 - Alarm Vacuum NG
            { "Alarm_Vacuum_Outfeed", 9028 },              // MW9028 - Alarm Vacuum Outfeed
            { "Alarm_Vacuum_Pickup_Tray", 9029 },          // MW9029 - Alarm Vacuum Pickup Tray
            { "Alarm_Vacuum_Inspect_1", 9030 },            // MW9030 - Alarm Vacuum Inspect 1
            { "Alarm_Vacuum_Inspect_2", 9031 },            // MW9031 - Alarm Vacuum Inspect 2
            
            // Unit Alarms
            { "Alarm_Infeed_Unit", 9040 },                 // MW9040 - Alarm Infeed Unit
            { "Alarm_Infeed_Cannot_Pick_Product", 9041 },  // MW9041 - Alarm Infeed can not pick Product
            { "Alarm_Infeed_Product_Falled", 9042 },       // MW9042 - Alarm Infeed Product Falled
            { "Alarm_Camera_1_Cannot_Take_Photo", 9043 },  // MW9043 - Alarm Camera 1 can not take a photo
            { "Alarm_Product_Input_Error", 9044 },         // MW9044 - Alarm Product Input Error
            { "Alarm_Infeed_Unit_ORG_Timeout", 9045 },     // MW9045 - Alarm Infeed Unit ORG timeout
            
            { "Alarm_Transfer_Unit", 9048 },               // MW9048 - Alarm Transfer Unit
            { "Alarm_Transfer_Cannot_Pick_Product", 9049 }, // MW9049 - Alarm Transfer can not pick Product
            { "Alarm_Transfer_Product_Falled", 9050 },     // MW9050 - Alarm Transfer Product Falled
            { "Alarm_Transfer_Unit_ORG_Timeout", 9051 },   // MW9051 - Alarm Transfer Unit ORG timeout
            
            { "Alarm_Outfeed_Unit", 9056 },                // MW9056 - Alarm Outfeed Unit
            { "Alarm_Outfeed_Cannot_Pick_Product", 9057 }, // MW9057 - Alarm Outfeed can not pick Product
            { "Alarm_Outfeed_Product_Falled", 9058 },      // MW9058 - Alarm Outfeed Product Falled
            { "Alarm_Outfeed_Unit_ORG_Timeout", 9059 },    // MW9059 - Alarm Outfeed Unit ORG timeout
            
            { "Alarm_Inspect_1_Unit", 9064 },              // MW9064 - Alarm Inspect 1 Unit
            { "Alarm_Inspect_1_Cannot_Hold_Product", 9065 }, // MW9065 - Alarm Inspect 1 can not hold Product
            { "Alarm_Camera_2_Cannot_Take_Photo", 9066 },  // MW9066 - Alarm Camera 2 can not take a photo
            { "Alarm_Inspect_1_Unit_ORG_Timeout", 9067 },  // MW9067 - Alarm Inspect 1 Unit ORG timeout
            
            { "Alarm_Inspect_2_Unit", 9072 },              // MW9072 - Alarm Inspect 2 Unit
            { "Alarm_Inspect_2_Cannot_Hold_Product", 9073 }, // MW9073 - Alarm Inspect 2 can not hold Product
            { "Alarm_Camera_3_Cannot_Take_Photo", 9074 },  // MW9074 - Alarm Camera 3 can not take a photo
            { "Alarm_Inspect_2_Unit_ORG_Timeout", 9075 },  // MW9075 - Alarm Inspect 2 Unit ORG timeout
            
            // Tray Supply Alarms
            { "Alarm_Supply_Tray_Unit", 9080 },            // MW9080 - Alarm Supply Tray Unit
            { "Alarm_Supply_Tray_Input_Empty", 9081 },     // MW9081 - Alarm Supply Tray Input Empty
            { "Alarm_Supply_Tray_Input_Over", 9082 },      // MW9082 - Alarm Supply Tray Input Over
            { "Alarm_Supply_Tray_Output_Empty", 9083 },    // MW9083 - Alarm Supply Tray Output Empty
            { "Alarm_Supply_Tray_Output_Full", 9084 },     // MW9084 - Alarm Supply Tray Output Full
            { "Alarm_Supply_Tray_Unit_ORG_Timeout", 9085 }, // MW9085 - Alarm Supply Tray Unit ORG timeout
            
            // NG Conveyor Alarms
            { "Alarm_NG_CV_Unit", 9090 },                  // MW9090 - Alarm NG CV Unit
            { "Alarm_NG_CV_Full", 9091 },                  // MW9091 - Alarm NG CV Full
        };

        public static readonly Dictionary<string, ushort> Servo_Status_Coils = new Dictionary<string, ushort>
        {
            { "MC_Power_OK", 2400 },
            { "ORG_Complete", 2401 },
            { "Inching_Complete", 2402 },
            { "Move_Complete", 2403 },
        };

        public static readonly Dictionary<string, ushort> ServoPositionData = new Dictionary<string, ushort>
        {
            { "X1_Pos1_Idle", 2000 },
            { "X1_Pos2_Pickup", 2004 },
            { "X1_Pos3_Place", 2008 },
            { "X1_Speed_Pos1", 2040 },
            { "X1_Speed_Pos2", 2044 },
            { "X1_Speed_Pos3", 2048 },
            { "Y1_Pos1_Idle", 2200 },
            { "Y1_Pos2_Pickup", 2204 },
            { "Y1_Pos3_Place", 2208 },
            { "Y1_Speed_Pos1", 2240 },
            { "Y1_Speed_Pos2", 2244 },
            { "Y1_Speed_Pos3", 2248 },
            { "R1_Pos1_Idle", 2400 },
            { "R1_Pos2_Pickup", 2404 },
            { "R1_Pos3_Place", 2408 },
            { "R1_Speed_Pos1", 2440 },
            { "R1_Speed_Pos2", 2444 },
            { "R1_Speed_Pos3", 2448 },
            { "X2_Pos1_Idle", 2600 },
            { "X2_Pos2_PreparePickup", 2604 },
            { "X2_Pos3_Pickup", 2608 },
            { "X2_Pos4_PreparePlace", 2612 },
            { "X2_Pos5_Place", 2616 },
            { "X2_Pos6_NGPosition", 2620 },
            { "X2_Speed_Pos1", 2640 },
            { "X2_Speed_Pos2", 2644 },
            { "X2_Speed_Pos3", 2648 },
            { "X2_Speed_Pos4", 2652 },
            { "X2_Speed_Pos5", 2656 },
            { "X2_Speed_Pos6", 2660 },
            { "Z2_Pos1_Idle", 2800 },
            { "Z2_Pos2_PreparePickup", 2804 },
            { "Z2_Pos3_Pickup", 2808 },
            { "Z2_Pos4_PreparePlace", 2812 },
            { "Z2_Pos5_Place", 2816 },
            { "Z2_Pos6_NGPosition", 2820 },
            { "Z2_Speed_Pos1", 2840 },
            { "Z2_Speed_Pos2", 2844 },
            { "Z2_Speed_Pos3", 2848 },
            { "Z2_Speed_Pos4", 2852 },
            { "Z2_Speed_Pos5", 2856 },
            { "Z2_Speed_Pos6", 2860 },
            { "X3_Pos1_Idle", 3000 },
            { "X3_Pos2_Pickup", 3004 },
            { "X3_Pos3_OKPlace1", 3008 },
            { "X3_Pos4_OKPlace2", 3012 },
            { "X3_Pos5_OKPlace3", 3016 },
            { "X3_Pos6_OKPlace4", 3020 },
            { "X3_Pos7_OKPlace5", 3024 },
            { "X3_Pos8_OKPlace6", 3028 },
            { "X3_Pos9_NGPlace", 3032 },
            { "X3_Pos10_PickupTray", 3036 },
            { "X3_Pos11_PlaceTray", 3040 },
            { "X3_Speed_Pos1", 3080 },
            { "X3_Speed_Pos2", 3084 },
            { "X3_Speed_Pos3", 3088 },
            { "X3_Speed_Pos4", 3092 },
            { "X3_Speed_Pos5", 3096 },
            { "X3_Speed_Pos6", 3100 },
            { "X3_Speed_Pos7", 3104 },
            { "X3_Speed_Pos8", 3108 },
            { "X3_Speed_Pos9", 3112 },
            { "X3_Speed_Pos10", 3116 },
            { "X3_Speed_Pos11", 3120 },
            { "Y3_Pos1_Idle", 3200 },
            { "Y3_Pos2_Pickup", 3204 },
            { "Y3_Pos3_OKPlace1", 3208 },
            { "Y3_Pos4_OKPlace2", 3212 },
            { "Y3_Pos5_OKPlace3", 3216 },
            { "Y3_Pos6_OKPlace4", 3220 },
            { "Y3_Pos7_OKPlace5", 3224 },
            { "Y3_Pos8_OKPlace6", 3228 },
            { "Y3_Pos9_NGPlace", 3232 },
            { "Y3_Pos10_PickupTray", 3236 },
            { "Y3_Pos11_PlaceTray", 3240 },
            { "Y3_Speed_Pos1", 3280 },
            { "Y3_Speed_Pos2", 3284 },
            { "Y3_Speed_Pos3", 3288 },
            { "Y3_Speed_Pos4", 3292 },
            { "Y3_Speed_Pos5", 3296 },
            { "Y3_Speed_Pos6", 3300 },
            { "Y3_Speed_Pos7", 3304 },
            { "Y3_Speed_Pos8", 3308 },
            { "Y3_Speed_Pos9", 3312 },
            { "Y3_Speed_Pos10", 3316 },
            { "Y3_Speed_Pos11", 3320 },
            { "Z4_Pos1_Idle", 3400 },
            { "Z4_Pos2_Focus1", 3404 },
            { "Z4_Pos3_Focus2", 3408 },
            { "Z4_Pos4_Focus3", 3412 },
            { "Z4_Speed_Pos1", 3440 },
            { "Z4_Speed_Pos2", 3444 },
            { "Z4_Speed_Pos3", 3448 },
            { "Z4_Speed_Pos4", 3452 },
            { "C4_Pos1_Idle", 3600 },
            { "C4_Pos2_Focus1", 3604 },
            { "C4_Pos3_Focus2", 3608 },
            { "C4_Pos4_Focus3", 3612 },
            { "C4_Speed_Pos1", 3640 },
            { "C4_Speed_Pos2", 3644 },
            { "C4_Speed_Pos3", 3648 },
            { "C4_Speed_Pos4", 3652 },
            { "Z5_Pos1_Idle", 3800 },
            { "Z5_Pos2_Focus1", 3804 },
            { "Z5_Pos3_Focus2", 3808 },
            { "Z5_Pos4_Focus3", 3812 },
            { "Z5_Pos5_Unload", 3816 },
            { "Z5_Speed_Pos1", 3840 },
            { "Z5_Speed_Pos2", 3844 },
            { "Z5_Speed_Pos3", 3848 },
            { "Z5_Speed_Pos4", 3852 },
            { "Z5_Speed_Pos5", 3856 },
            { "C5_Pos1_Idle", 4000 },
            { "C5_Pos2_Focus1", 4004 },
            { "C5_Pos3_Focus2", 4008 },
            { "C5_Pos4_Focus3", 4012 },
            { "C5_Pos5_Unload", 4016 },
            { "C5_Speed_Pos1", 4040 },
            { "C5_Speed_Pos2", 4044 },
            { "C5_Speed_Pos3", 4048 },
            { "C5_Speed_Pos4", 4052 },
            { "C5_Speed_Pos5", 4056 },
            { "Z61_Pos1_Home", 4200 },
            { "Z61_Offset", 4204 },
            { "Z61_Speed", 4240 },
            { "Z62_Pos1_Home", 4400 },
            { "Z62_Offset", 4404 },
            { "Z62_Speed", 4440 },
            { "CV7_StepDistance", 4600 },
            { "CV7_Speed", 4640 },
        };
    }
}
