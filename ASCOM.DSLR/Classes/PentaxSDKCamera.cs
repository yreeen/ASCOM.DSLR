using ASCOM.DSLR.Enums;
using ASCOM.DSLR.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using RCC = Ricoh.CameraController;
using System.IO.Ports;

namespace ASCOM.DSLR.Classes
{
    public class PentaxSDKCamera : BaseCamera, IDslrCamera
    {
        static readonly Dictionary<int, RCC.ISO> _IsoMap = new Dictionary<int, RCC.ISO>()
        {//for support high ISO, it use int type.
            {100, RCC.ISO.ISO100},
            {125, RCC.ISO.ISO125},
            {140, RCC.ISO.ISO140},
            {160, RCC.ISO.ISO160},
            {200, RCC.ISO.ISO200},
            {250, RCC.ISO.ISO250},
            {280, RCC.ISO.ISO280},
            {320, RCC.ISO.ISO320},
            {400, RCC.ISO.ISO400},
            {500, RCC.ISO.ISO500},
            {560, RCC.ISO.ISO560},
            {640, RCC.ISO.ISO640},
            {800, RCC.ISO.ISO800},
            {1000, RCC.ISO.ISO1000},
            {1100, RCC.ISO.ISO1100},
            {1250, RCC.ISO.ISO1250},
            {1600, RCC.ISO.ISO1600},
            {2000, RCC.ISO.ISO2000},
            {2200, RCC.ISO.ISO2200},
            {2500, RCC.ISO.ISO2500},
            {3200, RCC.ISO.ISO3200},
            {4000, RCC.ISO.ISO4000},
            {4500, RCC.ISO.ISO4500},
            {5000, RCC.ISO.ISO5000},
            {6400, RCC.ISO.ISO6400},
            {8000, RCC.ISO.ISO8000},
            {9000, RCC.ISO.ISO9000},
            {10000, RCC.ISO.ISO10000},
            {12800, RCC.ISO.ISO12800},
            {16000, RCC.ISO.ISO16000},
            {18000, RCC.ISO.ISO18000},
            {20000, RCC.ISO.ISO20000},
            {25600, RCC.ISO.ISO25600},
            {32000, RCC.ISO.ISO32000},
            {36000, RCC.ISO.ISO36000},
            {40000, RCC.ISO.ISO40000},
            {51200, RCC.ISO.ISO51200},
            {64000, RCC.ISO.ISO64000},
            {72000, RCC.ISO.ISO72000},
            {80000, RCC.ISO.ISO80000},
            {102400, RCC.ISO.ISO102400},
            {128000, RCC.ISO.ISO128000},
            {144000, RCC.ISO.ISO144000},
            {160000, RCC.ISO.ISO160000},
            {204800, RCC.ISO.ISO204800},
            {256000, RCC.ISO.ISO256000},
            {288000, RCC.ISO.ISO288000},
            {320000, RCC.ISO.ISO320000},
            {409600, RCC.ISO.ISO409600},
            {512000, RCC.ISO.ISO512000},
            {576000, RCC.ISO.ISO576000},
            {640000, RCC.ISO.ISO640000},
            {819200, RCC.ISO.ISO819200},
        };
        static readonly Dictionary<double, RCC.ShutterSpeed> _ShutterSpeedMapOrigin = new Dictionary<double, RCC.ShutterSpeed>()
        {
            {1200,RCC.ShutterSpeed.SS1200 },
            {1140,RCC.ShutterSpeed.SS1140 },
            {1080,RCC.ShutterSpeed.SS1080 },
            {1020,RCC.ShutterSpeed.SS1020 },
            {960,RCC.ShutterSpeed.SS960 },
            {900,RCC.ShutterSpeed.SS900 },
            {840,RCC.ShutterSpeed.SS840 },
            {780,RCC.ShutterSpeed.SS780 },
            {720,RCC.ShutterSpeed.SS720 },
            {660,RCC.ShutterSpeed.SS660 },
            {600,RCC.ShutterSpeed.SS600 },
            {540,RCC.ShutterSpeed.SS540 },
            {480,RCC.ShutterSpeed.SS480 },
            {420,RCC.ShutterSpeed.SS420 },
            {360,RCC.ShutterSpeed.SS360 },
            {300,RCC.ShutterSpeed.SS300 },
            {290,RCC.ShutterSpeed.SS290 },
            {280,RCC.ShutterSpeed.SS280 },
            {270,RCC.ShutterSpeed.SS270 },
            {260,RCC.ShutterSpeed.SS260 },
            {250,RCC.ShutterSpeed.SS250 },
            {240,RCC.ShutterSpeed.SS240 },
            {230,RCC.ShutterSpeed.SS230 },
            {220,RCC.ShutterSpeed.SS220 },
            {210,RCC.ShutterSpeed.SS210 },
            {200,RCC.ShutterSpeed.SS200 },
            {190,RCC.ShutterSpeed.SS190 },
            {180,RCC.ShutterSpeed.SS180 },
            {170,RCC.ShutterSpeed.SS170 },
            {160,RCC.ShutterSpeed.SS160 },
            {150,RCC.ShutterSpeed.SS150 },
            {140,RCC.ShutterSpeed.SS140 },
            {130,RCC.ShutterSpeed.SS130 },
            {120,RCC.ShutterSpeed.SS120 },
            {110,RCC.ShutterSpeed.SS110 },
            {100,RCC.ShutterSpeed.SS100 },
            {90,RCC.ShutterSpeed.SS90 },
            {80,RCC.ShutterSpeed.SS80 },
            {70,RCC.ShutterSpeed.SS70 },
            {60,RCC.ShutterSpeed.SS60 },
            {50,RCC.ShutterSpeed.SS50 },
            {40,RCC.ShutterSpeed.SS40 },
            {30,RCC.ShutterSpeed.SS30 },
            {25,RCC.ShutterSpeed.SS25 },
            {20,RCC.ShutterSpeed.SS20 },
            {15,RCC.ShutterSpeed.SS15 },
            {13,RCC.ShutterSpeed.SS13 },
            {10,RCC.ShutterSpeed.SS10 },
            {8,RCC.ShutterSpeed.SS8 },
            {6,RCC.ShutterSpeed.SS6 },
            {5,RCC.ShutterSpeed.SS5 },
            {4,RCC.ShutterSpeed.SS4 },
            {3.2,RCC.ShutterSpeed.SS32_10 },
            {3,RCC.ShutterSpeed.SS3 },
            {2.5,RCC.ShutterSpeed.SS25_10 },
            {2,RCC.ShutterSpeed.SS2 },
            {1.6,RCC.ShutterSpeed.SS16_10 },
            {1.5,RCC.ShutterSpeed.SS15_10 },
            {1.3,RCC.ShutterSpeed.SS13_10 },
            {1,RCC.ShutterSpeed.SS1 },
            {0.8,RCC.ShutterSpeed.SS8_10 },
            {0.7,RCC.ShutterSpeed.SS7_10 },
            {0.625,RCC.ShutterSpeed.SS10_16 },
            {0.6,RCC.ShutterSpeed.SS6_10 },
//            {0.5,RCC.ShutterSpeed.SS5_10 },
            {0.5,RCC.ShutterSpeed.SS1_2 },//重複キー注意
            {0.4,RCC.ShutterSpeed.SS4_10 },
            {1/3.0,RCC.ShutterSpeed.SS1_3 },
            {0.3,RCC.ShutterSpeed.SS3_10 },
            {0.25,RCC.ShutterSpeed.SS1_4 },
            {0.2,RCC.ShutterSpeed.SS1_5 },
            {0.166,RCC.ShutterSpeed.SS1_6 },
            {0.125,RCC.ShutterSpeed.SS1_8 },
            {0.1,RCC.ShutterSpeed.SS1_10 },
            {1/13.0,RCC.ShutterSpeed.SS1_13 },
            {1/15.0,RCC.ShutterSpeed.SS1_15 },
            {1/20.0,RCC.ShutterSpeed.SS1_20 },
            {1/25.0,RCC.ShutterSpeed.SS1_25 },
            {1/30.0,RCC.ShutterSpeed.SS1_30 },
            {1/40.0,RCC.ShutterSpeed.SS1_40 },
            {1/45.0,RCC.ShutterSpeed.SS1_45 },
            {1/50.0,RCC.ShutterSpeed.SS1_50 },
            {1/60.0,RCC.ShutterSpeed.SS1_60 },
            {1/80.0,RCC.ShutterSpeed.SS1_80 },
            {1/90.0,RCC.ShutterSpeed.SS1_90 },
            {1/100.0,RCC.ShutterSpeed.SS1_100 },
            {1/125.0,RCC.ShutterSpeed.SS1_125 },
            {1/160.0,RCC.ShutterSpeed.SS1_160 },
            {1/180.0,RCC.ShutterSpeed.SS1_180 },
            {1/200.0,RCC.ShutterSpeed.SS1_200 },
            {1/250.0,RCC.ShutterSpeed.SS1_250 },
            {1/320.0,RCC.ShutterSpeed.SS1_320 },
            {1/350.0,RCC.ShutterSpeed.SS1_350 },
            {1/400.0,RCC.ShutterSpeed.SS1_400 },
            {1/500.0,RCC.ShutterSpeed.SS1_500 },
            {1/640.0,RCC.ShutterSpeed.SS1_640 },
            {1/750.0,RCC.ShutterSpeed.SS1_750 },
            {1/800.0,RCC.ShutterSpeed.SS1_800 },
            {1/1000.0,RCC.ShutterSpeed.SS1_1000 },
            {1/1250.0,RCC.ShutterSpeed.SS1_1250 },
            {1/1500.0,RCC.ShutterSpeed.SS1_1500 },
            {1/1600.0,RCC.ShutterSpeed.SS1_1600 },
            {1/2000.0,RCC.ShutterSpeed.SS1_2000 },
            {1/2500.0,RCC.ShutterSpeed.SS1_2500 },
            {1/3000.0,RCC.ShutterSpeed.SS1_3000 },
            {1/3200.0,RCC.ShutterSpeed.SS1_3200 },
            {1/4000.0,RCC.ShutterSpeed.SS1_4000 },
            {1/5000.0,RCC.ShutterSpeed.SS1_5000 },
            {1/6000.0,RCC.ShutterSpeed.SS1_6000 },
            {1/6400.0,RCC.ShutterSpeed.SS1_6400 },
            {1/8000.0,RCC.ShutterSpeed.SS1_8000 },
            {1/10000.0,RCC.ShutterSpeed.SS1_10000 },
            {1/12000.0,RCC.ShutterSpeed.SS1_12000 },
            {1/12800.0,RCC.ShutterSpeed.SS1_12800 },
            {1/16000.0,RCC.ShutterSpeed.SS1_16000 },
            {1/20000.0,RCC.ShutterSpeed.SS1_20000 },
            {1/24000.0,RCC.ShutterSpeed.SS1_24000 },
        };
        private Dictionary<double, RCC.ShutterSpeed> _ShutterSpeedMap = new Dictionary<double, RCC.ShutterSpeed>(_ShutterSpeedMapOrigin);
        static Dictionary<string, KeyValuePair<double, RCC.ShutterSpeed>> _ShutterSpeedStr2KVPs = new Dictionary<string, KeyValuePair<double, RCC.ShutterSpeed>>()
        {
            { "1200",new KeyValuePair<double, RCC.ShutterSpeed>(1200,RCC.ShutterSpeed.SS1200)},
            { "1140",new KeyValuePair<double, RCC.ShutterSpeed>(1140,RCC.ShutterSpeed.SS1140)},
            { "1080",new KeyValuePair<double, RCC.ShutterSpeed>(1080,RCC.ShutterSpeed.SS1080)},
            { "1020",new KeyValuePair<double, RCC.ShutterSpeed>(1020,RCC.ShutterSpeed.SS1020)},
            { "960",new KeyValuePair<double, RCC.ShutterSpeed>(960,RCC.ShutterSpeed.SS960)},
            { "900",new KeyValuePair<double, RCC.ShutterSpeed>(900,RCC.ShutterSpeed.SS900)},
            { "840",new KeyValuePair<double, RCC.ShutterSpeed>(840,RCC.ShutterSpeed.SS840)},
            { "780",new KeyValuePair<double, RCC.ShutterSpeed>(780,RCC.ShutterSpeed.SS780)},
            { "720",new KeyValuePair<double, RCC.ShutterSpeed>(720,RCC.ShutterSpeed.SS720)},
            { "660",new KeyValuePair<double, RCC.ShutterSpeed>(660,RCC.ShutterSpeed.SS660)},
            { "600",new KeyValuePair<double, RCC.ShutterSpeed>(600,RCC.ShutterSpeed.SS600)},
            { "540",new KeyValuePair<double, RCC.ShutterSpeed>(540,RCC.ShutterSpeed.SS540)},
            { "480",new KeyValuePair<double, RCC.ShutterSpeed>(480,RCC.ShutterSpeed.SS480)},
            { "420",new KeyValuePair<double, RCC.ShutterSpeed>(420,RCC.ShutterSpeed.SS420)},
            { "360",new KeyValuePair<double, RCC.ShutterSpeed>(400,RCC.ShutterSpeed.SS360)},
            { "300",new KeyValuePair<double, RCC.ShutterSpeed>(300,RCC.ShutterSpeed.SS300)},
            { "290",new KeyValuePair<double, RCC.ShutterSpeed>(290,RCC.ShutterSpeed.SS290)},
            { "280",new KeyValuePair<double, RCC.ShutterSpeed>(280,RCC.ShutterSpeed.SS280)},
            { "270",new KeyValuePair<double, RCC.ShutterSpeed>(270,RCC.ShutterSpeed.SS270)},
            { "260",new KeyValuePair<double, RCC.ShutterSpeed>(260,RCC.ShutterSpeed.SS260)},
            { "250",new KeyValuePair<double, RCC.ShutterSpeed>(250,RCC.ShutterSpeed.SS250)},
            { "240",new KeyValuePair<double, RCC.ShutterSpeed>(240,RCC.ShutterSpeed.SS240)},
            { "230",new KeyValuePair<double, RCC.ShutterSpeed>(230,RCC.ShutterSpeed.SS230)},
            { "220",new KeyValuePair<double, RCC.ShutterSpeed>(220,RCC.ShutterSpeed.SS220)},
            { "210",new KeyValuePair<double, RCC.ShutterSpeed>(210,RCC.ShutterSpeed.SS210)},
            { "200",new KeyValuePair<double, RCC.ShutterSpeed>(200,RCC.ShutterSpeed.SS200)},
            { "190",new KeyValuePair<double, RCC.ShutterSpeed>(190,RCC.ShutterSpeed.SS190)},
            { "180",new KeyValuePair<double, RCC.ShutterSpeed>(180,RCC.ShutterSpeed.SS180)},
            { "170",new KeyValuePair<double, RCC.ShutterSpeed>(170,RCC.ShutterSpeed.SS170)},
            { "160",new KeyValuePair<double, RCC.ShutterSpeed>(160,RCC.ShutterSpeed.SS160)},
            { "150",new KeyValuePair<double, RCC.ShutterSpeed>(150,RCC.ShutterSpeed.SS150)},
            { "140",new KeyValuePair<double, RCC.ShutterSpeed>(140,RCC.ShutterSpeed.SS140)},
            { "130",new KeyValuePair<double, RCC.ShutterSpeed>(130,RCC.ShutterSpeed.SS130)},
            { "120",new KeyValuePair<double, RCC.ShutterSpeed>(120,RCC.ShutterSpeed.SS120)},
            { "110",new KeyValuePair<double, RCC.ShutterSpeed>(110,RCC.ShutterSpeed.SS110)},
            { "100",new KeyValuePair<double, RCC.ShutterSpeed>(100,RCC.ShutterSpeed.SS100)},
            { "90",new KeyValuePair<double, RCC.ShutterSpeed>(90,RCC.ShutterSpeed.SS90)},
            { "80",new KeyValuePair<double, RCC.ShutterSpeed>(80,RCC.ShutterSpeed.SS80)},
            { "70",new KeyValuePair<double, RCC.ShutterSpeed>(70,RCC.ShutterSpeed.SS70)},
            { "60",new KeyValuePair<double, RCC.ShutterSpeed>(60,RCC.ShutterSpeed.SS60)},
            { "50",new KeyValuePair<double, RCC.ShutterSpeed>(50,RCC.ShutterSpeed.SS50)},
            { "40",new KeyValuePair<double, RCC.ShutterSpeed>(40,RCC.ShutterSpeed.SS40)},
            { "30",new KeyValuePair<double, RCC.ShutterSpeed>(30,RCC.ShutterSpeed.SS30)},
            { "25",new KeyValuePair<double, RCC.ShutterSpeed>(25,RCC.ShutterSpeed.SS25)},
            { "20",new KeyValuePair<double, RCC.ShutterSpeed>(20,RCC.ShutterSpeed.SS20)},
            { "15",new KeyValuePair<double, RCC.ShutterSpeed>(15,RCC.ShutterSpeed.SS15)},
            { "13",new KeyValuePair<double, RCC.ShutterSpeed>(13,RCC.ShutterSpeed.SS13)},
            { "10",new KeyValuePair<double, RCC.ShutterSpeed>(10,RCC.ShutterSpeed.SS10)},
            { "8",new KeyValuePair<double, RCC.ShutterSpeed>(8,RCC.ShutterSpeed.SS8)},
            { "6",new KeyValuePair<double, RCC.ShutterSpeed>(6,RCC.ShutterSpeed.SS6)},
            { "5",new KeyValuePair<double, RCC.ShutterSpeed>(5,RCC.ShutterSpeed.SS5)},
            { "4",new KeyValuePair<double, RCC.ShutterSpeed>(4,RCC.ShutterSpeed.SS4)},
            { "3.2",new KeyValuePair<double, RCC.ShutterSpeed>(3.2,RCC.ShutterSpeed.SS32_10)},
            { "3",new KeyValuePair<double, RCC.ShutterSpeed>(3,RCC.ShutterSpeed.SS3)},
            { "2.5",new KeyValuePair<double, RCC.ShutterSpeed>(2.5,RCC.ShutterSpeed.SS25_10)},
            { "2",new KeyValuePair<double, RCC.ShutterSpeed>(2,RCC.ShutterSpeed.SS2)},
            { "1.6",new KeyValuePair<double, RCC.ShutterSpeed>(1.6,RCC.ShutterSpeed.SS16_10)},
            { "1.5",new KeyValuePair<double, RCC.ShutterSpeed>(1.5,RCC.ShutterSpeed.SS15_10)},
            { "1.3",new KeyValuePair<double, RCC.ShutterSpeed>(1.3,RCC.ShutterSpeed.SS13_10)},
            { "1",new KeyValuePair<double, RCC.ShutterSpeed>(1,RCC.ShutterSpeed.SS1)},
            { "0.8",new KeyValuePair<double, RCC.ShutterSpeed>(0.8,RCC.ShutterSpeed.SS8_10)},
            { "0.7",new KeyValuePair<double, RCC.ShutterSpeed>(0.7,RCC.ShutterSpeed.SS7_10)},
            { "0.625",new KeyValuePair<double, RCC.ShutterSpeed>(0.625,RCC.ShutterSpeed.SS10_16)},
            { "0.6",new KeyValuePair<double, RCC.ShutterSpeed>(0.6,RCC.ShutterSpeed.SS6_10)},
//            { "0.5",new KeyValuePair<double, RCC.ShutterSpeed>(0.5,RCC.ShutterSpeed.SS5_10)},
            { "0.5",new KeyValuePair<double, RCC.ShutterSpeed>(0.5,RCC.ShutterSpeed.SS1_2)},
            { "0.4",new KeyValuePair<double, RCC.ShutterSpeed>(0.4,RCC.ShutterSpeed.SS4_10)},
            { "1/3",new KeyValuePair<double, RCC.ShutterSpeed>(1/3.0,RCC.ShutterSpeed.SS1_3)},
            { "0.3",new KeyValuePair<double, RCC.ShutterSpeed>(0.3,RCC.ShutterSpeed.SS3_10)},
            { "1/4",new KeyValuePair<double, RCC.ShutterSpeed>(0.25,RCC.ShutterSpeed.SS1_4)},
            { "1/5",new KeyValuePair<double, RCC.ShutterSpeed>(0.2,RCC.ShutterSpeed.SS1_5)},
            { "1/6",new KeyValuePair<double, RCC.ShutterSpeed>(0.166,RCC.ShutterSpeed.SS1_6)},
            { "1/8",new KeyValuePair<double, RCC.ShutterSpeed>(0.125,RCC.ShutterSpeed.SS1_8)},
            { "1/10",new KeyValuePair<double, RCC.ShutterSpeed>(0.1,RCC.ShutterSpeed.SS1_10)},
            { "1/13",new KeyValuePair<double, RCC.ShutterSpeed>(1/13.0,RCC.ShutterSpeed.SS1_13)},
            { "1/15",new KeyValuePair<double, RCC.ShutterSpeed>(1/15.0,RCC.ShutterSpeed.SS1_15)},
            { "1/20",new KeyValuePair<double, RCC.ShutterSpeed>(1/20.0,RCC.ShutterSpeed.SS1_20)},
            { "1/25",new KeyValuePair<double, RCC.ShutterSpeed>(1/25.0,RCC.ShutterSpeed.SS1_25)},
            { "1/30",new KeyValuePair<double, RCC.ShutterSpeed>(1/30.0,RCC.ShutterSpeed.SS1_30)},
            { "1/40",new KeyValuePair<double, RCC.ShutterSpeed>(1/40.0,RCC.ShutterSpeed.SS1_40)},
            { "1/45",new KeyValuePair<double, RCC.ShutterSpeed>(1/45.0,RCC.ShutterSpeed.SS1_45)},
            { "1/50",new KeyValuePair<double, RCC.ShutterSpeed>(1/50.0,RCC.ShutterSpeed.SS1_50)},
            { "1/60",new KeyValuePair<double, RCC.ShutterSpeed>(1/60.0,RCC.ShutterSpeed.SS1_60)},
            { "1/80",new KeyValuePair<double, RCC.ShutterSpeed>(1/80.0,RCC.ShutterSpeed.SS1_80)},
            { "1/90",new KeyValuePair<double, RCC.ShutterSpeed>(1/90.0,RCC.ShutterSpeed.SS1_90)},
            { "1/100",new KeyValuePair<double, RCC.ShutterSpeed>(1/100.0,RCC.ShutterSpeed.SS1_100)},
            { "1/125",new KeyValuePair<double, RCC.ShutterSpeed>(1/125.0,RCC.ShutterSpeed.SS1_125)},
            { "1/160",new KeyValuePair<double, RCC.ShutterSpeed>(1/160.0,RCC.ShutterSpeed.SS1_160)},
            { "1/180",new KeyValuePair<double, RCC.ShutterSpeed>(1/180.0,RCC.ShutterSpeed.SS1_180)},
            { "1/200",new KeyValuePair<double, RCC.ShutterSpeed>(1/200.0,RCC.ShutterSpeed.SS1_200)},
            { "1/250",new KeyValuePair<double, RCC.ShutterSpeed>(1/250.0,RCC.ShutterSpeed.SS1_250)},
            { "1/320",new KeyValuePair<double, RCC.ShutterSpeed>(1/320.0,RCC.ShutterSpeed.SS1_320)},
            { "1/350",new KeyValuePair<double, RCC.ShutterSpeed>(1/350.0,RCC.ShutterSpeed.SS1_350)},
            { "1/400",new KeyValuePair<double, RCC.ShutterSpeed>(1/400.0,RCC.ShutterSpeed.SS1_400)},
            { "1/500",new KeyValuePair<double, RCC.ShutterSpeed>(1/500.0,RCC.ShutterSpeed.SS1_500)},
            { "1/640",new KeyValuePair<double, RCC.ShutterSpeed>(1/640.0,RCC.ShutterSpeed.SS1_640)},
            { "1/750",new KeyValuePair<double, RCC.ShutterSpeed>(1/750.0,RCC.ShutterSpeed.SS1_750)},
            { "1/800",new KeyValuePair<double, RCC.ShutterSpeed>(1/800.0,RCC.ShutterSpeed.SS1_800)},
            { "1/1000",new KeyValuePair<double, RCC.ShutterSpeed>(1/1000.0,RCC.ShutterSpeed.SS1_1000)},
            { "1/1250",new KeyValuePair<double, RCC.ShutterSpeed>(1/1250.0,RCC.ShutterSpeed.SS1_1250)},
            { "1/1500",new KeyValuePair<double, RCC.ShutterSpeed>(1/1500.0,RCC.ShutterSpeed.SS1_1500)},
            { "1/1600",new KeyValuePair<double, RCC.ShutterSpeed>(1/1600.0,RCC.ShutterSpeed.SS1_1600)},
            { "1/2000",new KeyValuePair<double, RCC.ShutterSpeed>(1/2000.0,RCC.ShutterSpeed.SS1_2000)},
            { "1/2500",new KeyValuePair<double, RCC.ShutterSpeed>(1/2500.0,RCC.ShutterSpeed.SS1_2500)},
            { "1/3000",new KeyValuePair<double, RCC.ShutterSpeed>(1/3000.0,RCC.ShutterSpeed.SS1_3000)},
            { "1/3200",new KeyValuePair<double, RCC.ShutterSpeed>(1/3200.0,RCC.ShutterSpeed.SS1_3200)},
            { "1/4000",new KeyValuePair<double, RCC.ShutterSpeed>(1/4000.0,RCC.ShutterSpeed.SS1_4000)},
            { "1/5000",new KeyValuePair<double, RCC.ShutterSpeed>(1/5000.0,RCC.ShutterSpeed.SS1_5000)},
            { "1/6000",new KeyValuePair<double, RCC.ShutterSpeed>(1/6000.0,RCC.ShutterSpeed.SS1_6000)},
            { "1/6400",new KeyValuePair<double, RCC.ShutterSpeed>(1/6400.0,RCC.ShutterSpeed.SS1_6400)},
            { "1/8000",new KeyValuePair<double, RCC.ShutterSpeed>(1/8000.0,RCC.ShutterSpeed.SS1_8000)},
            { "1/10000",new KeyValuePair<double, RCC.ShutterSpeed>(1/10000.0,RCC.ShutterSpeed.SS1_10000)},
            { "1/12000",new KeyValuePair<double, RCC.ShutterSpeed>(1/12000.0,RCC.ShutterSpeed.SS1_12000)},
            { "1/12800",new KeyValuePair<double, RCC.ShutterSpeed>(1/12800.0,RCC.ShutterSpeed.SS1_12800)},
            { "1/16000",new KeyValuePair<double, RCC.ShutterSpeed>(1/16000.0,RCC.ShutterSpeed.SS1_16000)},
            { "1/20000",new KeyValuePair<double, RCC.ShutterSpeed>(1/20000.0,RCC.ShutterSpeed.SS1_20000)},
            { "1/24000",new KeyValuePair<double, RCC.ShutterSpeed>(1/24000.0,RCC.ShutterSpeed.SS1_24000)},
        };                                                         

        private RCC.ISO int2iso(int iso)
        {
            if (iso < 100) iso = 100;
            if (_IsoMap.ContainsKey(iso))
                return _IsoMap[iso];
            throw new InvalidValueException("ISO " + iso + " is not supported. int2iso();");

        }
        private int iso2int(RCC.ISO rcciso)
        {
            if (_IsoMap.ContainsValue(rcciso))
            {
                var pair = _IsoMap.FirstOrDefault(c => c.Value == rcciso);
                return pair.Key;
            }
            throw new InvalidValueException("ISO " + rcciso.ToString() + " is not supported. iso2int();");
        }

        private RCC.ShutterSpeed double2ss(double dss)
        {
            if (_ShutterSpeedMap.ContainsKey(dss))
                return _ShutterSpeedMap[dss];

            var result = new KeyValuePair<double, RCC.ShutterSpeed>(1000000,RCC.ShutterSpeed.Auto);

            foreach (KeyValuePair<double,RCC.ShutterSpeed> kvp in _ShutterSpeedMap)
            {
                if (Math.Abs(kvp.Key - dss) <= Math.Abs(result.Key - dss))
                    result = kvp;
            }

            if (result.Value == RCC.ShutterSpeed.Auto)
            {
                throw new InvalidValueException("Shutter Speed " + dss + "is not supported. double2ss(); 01");
            }
            if(Math.Abs(dss - result.Key) > Math.Abs(dss) * 0.8)
            {
                throw new InvalidValueException("Shutter Speed " + dss + "is not supported. double2ss(); 02");
            }

            return result.Value;

            
            throw new InvalidValueException("SS " + dss + " is not supported. ss2double();");
        }

        private double ss2double(RCC.ShutterSpeed ss)
        {
            if(_ShutterSpeedMap.ContainsValue(ss))
            {
                var pair = _ShutterSpeedMap.FirstOrDefault(c => c.Value == ss);
                return pair.Key;
            }
            throw new InvalidValueException("SS " + ss.ToString() + " is not supported. ss2double();");
        }

        public PentaxSDKCamera(List<CameraModel> cameraModelsHistory) : base(cameraModelsHistory)
        {
            ScanCameras();
        }

        public event EventHandler<ImageReadyEventArgs> ImageReady;
        public event EventHandler<ExposureFailedEventArgs> ExposureFailed;
        public event EventHandler<LiveViewImageReadyEventArgs> LiveViewImageReady;
        private bool _liveViewEnabled = false;

        private RCC.DeviceInterface _deviceInterface = RCC.DeviceInterface.USB;

        private RCC.CameraDevice _connectedCameraDevice = null;

        private List<RCC.CameraDevice> _detectedCameraDevices;
        private List<RCC.CameraDevice> DetectedCameraDevices
        {
            get
            {
                if (_detectedCameraDevices == null)
                {
                    _detectedCameraDevices = new List<RCC.CameraDevice>(RCC.CameraDeviceDetector.Detect(_deviceInterface));
                }
                return _detectedCameraDevices;
            }
            set => _detectedCameraDevices = value;
        }

        private string _modelStr;

        public string Model
        {
            get
            {
                if (string.IsNullOrEmpty(_modelStr))
                {
                    if (DetectedCameraDevices.Count() != 0)
                    {
                        _modelStr = DetectedCameraDevices[0].Model;
                    }
                }
                return _modelStr;
            }
        }

        public ConnectionMethod IntegrationApi => ConnectionMethod.PentaxSDK;

        public bool SupportsViewView { get { return false; } }

        public void AbortExposure()
        {
            if (_connectedCameraDevice == null) return;
            if (_liveViewEnabled)
            {
                _connectedCameraDevice.StopLiveView();
                _liveViewEnabled = false;
            }
            RCC.Response response = _connectedCameraDevice.StopCapture();
            Logger.WriteTraceMessage("AbortExposure(); called. response is " + response.Result.ToString());
        }

        public void ConnectCamera()
        {
            if (_connectedCameraDevice != null) return;
            if(DetectedCameraDevices.Count() == 0)
            {
                Logger.WriteTraceMessage("ConnectCamera(); Failed. no camera device detected.");
                throw new NotConnectedException("ConnectCamera Faild. no camera device detected.");

            }
            
            var cameraDevice = DetectedCameraDevices.First();
            RCC.Response response = cameraDevice.Connect(_deviceInterface);

            if (!cameraDevice.IsConnected(_deviceInterface))
            {
                _connectedCameraDevice = null;
                Logger.WriteTraceMessage("ConnectCamera(); Failed.");
                throw new NotConnectedException("ConnectCamera Faild.");
            }
            _connectedCameraDevice = cameraDevice;
            _connectedCameraDevice.EventListeners.Add(new EventListner(this));

            var iso = new RCC.ISO();
            var ss = new RCC.ShutterSpeed();
            _connectedCameraDevice.GetCaptureSettings(new List<RCC.CaptureSetting>() { iso ,ss,});

            SimpleISOList = null;
            SimpleISOList = new List<short>();
            foreach(var i in iso.AvailableSettings)
            {
                if (i == RCC.ISO.Auto)
                    continue;
                int value = System.Convert.ToInt32(i.Value.ToString());
                if (value > 32000) continue;//privent over flow.
                SimpleISOList.Add((short)value);

            }

            if (ss.AvailableSettings.Count() > 0)
            {
                _ShutterSpeedMap.Clear();
                foreach (var s in ss.AvailableSettings)
                {
                    if (s.Value.ToString() == "Auto")
                        continue;
                    var member = _ShutterSpeedStr2KVPs[s.Value.ToString()];
                    _ShutterSpeedMap.Add(member.Key, member.Value);
                }
            }

            //get live-view image size.
            var lv = new RCC.LiveViewSpecification();
            _connectedCameraDevice.GetCameraDeviceSettings(new List<RCC.CameraDeviceSetting>() { lv });
            //{Image: 720x480, FocusArea: (0.1, 0.166666)(0.9, 0.166666)(0.9, 0.833333)(0.1, 0.833333)} k1mk2 result.
            //this parser was checked only K-1 MarkII.
            string lvstr = lv.Value.ToString();
            lvstr = lvstr.Substring(0, lvstr.IndexOf(","));
            lvstr = lvstr.Substring(lvstr.IndexOf(" "));
            LvFrameHeight = System.Convert.ToInt32(lvstr.Substring(lvstr.IndexOf("x") + 1));
            LvFrameWidth = System.Convert.ToInt32(lvstr.Substring(0, lvstr.IndexOf("x")));

        }

        public void DisconnectCamera()
        {
            if (_connectedCameraDevice == null) return;
            if (_liveViewEnabled) _connectedCameraDevice.StopLiveView();
            _connectedCameraDevice.EventListeners.Clear();
            RCC.Response resoponse = _connectedCameraDevice.Disconnect(_deviceInterface);
            _connectedCameraDevice = null;            
        }

        public void Dispose()
        {
            StopExposure();
            DisconnectCamera();
        }

        public override CameraModel ScanCameras()
        {
            CameraModel cameraModel = null;
            cameraModel = GetCameraModel(Model);

            if (cameraModel == null)
            {
                throw new NotConnectedException(ErrorMessages.NotConnected);
            }

            if (cameraModel.Name == "PENTAX K-1" || cameraModel.Name == "PENTAX K-1 Mark II")
            {//Pentax k-1 (mk2)
                cameraModel.SensorWidth = 35.9;
                cameraModel.SensorHeight = 24.0;
            }
            else if (cameraModel.Name == "PENTAX 645Z")
            {//Pentax 645Z
                cameraModel.SensorWidth = 43.8;
                cameraModel.SensorHeight = 32.8;
            }
            else
            {//Pentax KP or other.
                cameraModel.SensorWidth = 23.5;
                cameraModel.SensorHeight = 15.6;
            }

            return cameraModel;
        }

        class EventListner : RCC.CameraEventListener
        {
            private PentaxSDKCamera pentaxSDKCamera { get; set; }
            public EventListner(PentaxSDKCamera pentaxSDKCamera)
            {
                this.pentaxSDKCamera = pentaxSDKCamera;
            }
            public override void ImageAdded(RCC.CameraDevice sender, RCC.CameraImage image)
            {
                if (image.Type != RCC.ImageType.StillImage) return;
                
                if(pentaxSDKCamera.ImageFormat == ImageFormat.RAW && (image.Format == RCC.ImageFormat.DNG || image.Format == RCC.ImageFormat.PEF)
                   ||
                   pentaxSDKCamera.ImageFormat == ImageFormat.JPEG && image.Format == RCC.ImageFormat.JPEG)
                {
                    string fileName = pentaxSDKCamera.StorePath + image.Name;

                    using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                    {
                        var imageGetResponse = image.GetData(fs);
                        if (imageGetResponse.Result == RCC.Result.OK)
                        {
                            fs.Close();
                        }
                        else
                        {
                            foreach (var error in imageGetResponse.Errors)
                            {
                                //     Console.WriteLine("Error Code: " + error.Code.ToString() + " / Error Message: " + error.Message);
                            }
                            fs.Close();
                            throw new InvalidOperationException("image data cannot write into file.");
                        }
                    }
                    pentaxSDKCamera.ImageReady?.Invoke(pentaxSDKCamera, new ImageReadyEventArgs(fileName));
                }
            }
            public override void LiveViewFrameUpdated(RCC.CameraDevice sender, byte[] liveViewFrame)
            {
                using (var ms = new MemoryStream(liveViewFrame)) 
                {
                    var bitmap = new System.Drawing.Bitmap(ms);
                    pentaxSDKCamera.LiveViewImageReady?.Invoke(pentaxSDKCamera, new LiveViewImageReadyEventArgs(bitmap));
                } 
            }
        }

        public void StartExposure(double Duration, bool Light)
        {
            Logger.WriteTraceMessage("PentaxSDKCamera.StartExposure(Duration, Light), duration ='" + Duration.ToString() + "', Light = '" + Light.ToString() + "'");
            var iso = int2iso(Iso);
            var shutterspeed = double2ss(Duration);

            //RICOH Camera SDK 1.10
            // IS NOT SUPPORT BULB CAPTURE!!!!!

            //var exposureProgram = new RCC.ExposureProgram();
            //_connectedCameraDevice.GetCaptureSettings(new List<RCC.CaptureSetting> { exposureProgram });

            //if (exposureProgram.Equals(RCC.ExposureProgram.Bulb) && IsLiveViewMode == false)
            //{ 
            //    _connectedCameraDevice.SetCaptureSettings(new List<RCC.CaptureSetting>() {iso, RCC.ShutterSpeed.Bulb });

            //    RCC.StartCaptureResponse startCaptureResponse = _connectedCameraDevice.StartCapture(false);
            //    if (startCaptureResponse.Result == RCC.Result.Error)
            //    {
            //        if (ExposureFailed != null) ExposureFailed(this, new ExposureFailedEventArgs(""));
            //        return;
            //    }
            //    Thread.Sleep(TimeSpan.FromSeconds(Duration));
            //    StopExposure();
            //    return;
            //}
            if (Duration > 30)
            {
                //Bulbモードになってるか確認
                //なってれば外部シャッターのシリアルポート越しにシャッターを切る
                var mode = new RCC.ExposureProgram();
                _connectedCameraDevice.GetCaptureSettings(new List<RCC.CaptureSetting>() { mode});
                if(mode == RCC.ExposureProgram.Manual)
                {
                    //外部シャッターでの撮影を試みる
                    ThreadPool.QueueUserWorkItem(
                        state => 
                        {
                            SerialPort sp = new SerialPort(ExternalShutterPort,9600,Parity.None,8,StopBits.One);
                            try
                            {
                                sp.Open();
                                Thread.Sleep(100);
                                sp.Write(":Eb#");
                                Thread.Sleep((int)(Duration * 1000));
                                sp.Write(":Ee#");
                                sp.Close();
                            }
                            catch(Exception ex)
                            {
                                System.Windows.Forms.MessageBox.Show(ex.Message);
                            }
                        });
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Set exposure program as Bulb.");

                    return;
                }

            }
            else
            {
                if (Duration >= 30.0)
                    shutterspeed = RCC.ShutterSpeed.SS30;
                _connectedCameraDevice.SetCaptureSettings(new List<RCC.CaptureSetting>() { iso, shutterspeed });


                if (IsLiveViewMode)
                {
                    if (!_liveViewEnabled)
                    {
                        var StartLiveViewResponse = _connectedCameraDevice.StartLiveView();

                        if (StartLiveViewResponse.Result == RCC.Result.Error)//try again.
                        {
                            Task.WaitAll(Task.Delay(300));
                            StartLiveViewResponse = _connectedCameraDevice.StartLiveView();
                        }

                        if (StartLiveViewResponse.Result == RCC.Result.OK)
                            _liveViewEnabled = true;
                        else
                            _liveViewEnabled = false;
                    }

                }
                else
                {


                    RCC.StartCaptureResponse startCaptureResponse = _connectedCameraDevice.StartCapture(false);
                    if (startCaptureResponse.Result == RCC.Result.Error)
                    {
                        if (ExposureFailed != null) ExposureFailed(this, new ExposureFailedEventArgs(""));
                    }
                }
            }
            return;
        }

        public void StopExposure()
        {
            AbortExposure();
        }
    }
}
