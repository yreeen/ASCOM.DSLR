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

namespace ASCOM.DSLR.Classes
{
    public class PentaxSDKCamera : BaseCamera, IDslrCamera
    {
        readonly Dictionary<int, RCC.ISO> _IsoMap = new Dictionary<int, RCC.ISO>()
        {//shortだと高感度がカバーできないのでint型
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
        readonly Dictionary<double, RCC.ShutterSpeed> _ShutterSpeedMap = new Dictionary<double, RCC.ShutterSpeed>()
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
            {0.333,RCC.ShutterSpeed.SS1_3 },
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

        private RCC.ISO int2iso(int iso)
        {
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

            //TODO
            //double値で直接ヒットしなかった場合、最も近いキーを探す
            //ただし、もっとも近いキーとの差が３％を超えるのであれば例外を投げること
            
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

        private bool _waitingForImage = false;
        private DateTime _exposureStartTime;
        private const int timeout = 60;
        private double _lastDuration;
        private string _lastFileName;

        public PentaxSDKCamera(List<CameraModel> cameraModelsHistory) : base(cameraModelsHistory)
        {
            ScanCameras();
            ConnectCamera();
        }

        public event EventHandler<ImageReadyEventArgs> ImageReady;
        public event EventHandler<ExposureFailedEventArgs> ExposureFailed;
        public event EventHandler<LiveViewImageReadyEventArgs> LiveViewImageReady;

        private RCC.DeviceInterface _deviceInterface = RCC.DeviceInterface.USB;

        private RCC.CameraDevice _connectedCameraDevice = null;

        private List<RCC.CameraDevice> _detectedCameraDevices;
        private List<RCC.CameraDevice> DetectedCameraDevices
        {
            get
            {
                if (_detectedCameraDevices == null)
                {
//                    RCC.DeviceInterface deviceInterface = RCC.DeviceInterface.USB;
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

        }

        public void ConnectCamera()
        {
            if (_connectedCameraDevice != null)
                DisconnectCamera();
            _connectedCameraDevice = null;
            _connectedCameraDevice = DetectedCameraDevices.First();
            RCC.Response response = _connectedCameraDevice.Connect(_deviceInterface);

            if (!_connectedCameraDevice.IsConnected(_deviceInterface))
            {
                _connectedCameraDevice = null;
                throw new NotConnectedException("ConnectCamera Faild.");
            }

            var iso = new RCC.ISO();
            _connectedCameraDevice.GetCaptureSettings(new List<RCC.CaptureSetting>() { iso });
            //SimpleISOListに使用可能なISO値を追加する。ただし、short型での実装なのでISO32000までしか登録してはいけない。
            SimpleISOList = null;
            SimpleISOList = new List<short>();
            foreach(var i in iso.AvailableSettings)
            {
                if (i == RCC.ISO.Auto)
                    continue;
                int value = System.Convert.ToInt32(i.Value.ToString());
                if (value > 32000) continue;
                SimpleISOList.Add((short)value);

            }
        }

        public void DisconnectCamera()
        {
            RCC.Response resoponse = _connectedCameraDevice.Disconnect(_deviceInterface);
            _connectedCameraDevice = null;
        }

        public void Dispose()
        {

        }

        public override CameraModel ScanCameras()
        {
            CameraModel cameraModel = null;
            cameraModel = GetCameraModel(Model);

//            if (!string.IsNullOrEmpty(_modelStr))
//            {
//                cameraModel = GetCameraModel(Model);
//            }
            if (cameraModel == null)
            {
                throw new NotConnectedException(ErrorMessages.NotConnected);
            }

            //            RCC.DeviceInterface deviceInterface = RCC.DeviceInterface.USB;
            //          List<RCC.CameraDevice> detectedCameraDevices =
            //            RCC.CameraDeviceDetector.Detect(deviceInterface);
            //            CameraModel cameraModel = new CameraModel
            //            {
            //                Name = detectedCameraDevices[0].Model,
            //                SensorWidth = 23.5,
            //                SensorHeight = 15.6,
            //                ImageHeight = 0,
            //                ImageWidth = 0,
            //            };

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
            {//Pentax KP
                cameraModel.SensorWidth = 23.5;
                cameraModel.SensorHeight = 15.6;
            }

            return cameraModel;
        }

        public void StartExposure(double Duration, bool Light)
        {
            

            Logger.WriteTraceMessage("PentaxSDKCamera.StartExposure(Duration, Light), duration ='" + Duration.ToString() + "', Light = '" + Light.ToString() + "'");

            string fileName = StorePath + "\\" + GetFileName(Duration, DateTime.Now);
            MarkWaitingForExposure(Duration, fileName);
            watch();
            //データ受信用のイベントリスナーを設定する


            //Iso,Durationを設定する
            var iso = int2iso(Iso);
            var shutterspeed = double2ss(Duration);
            _connectedCameraDevice.SetCaptureSettings(new List<RCC.CaptureSetting>() {iso,shutterspeed });
            RCC.StartCaptureResponse startCaptureResponse = _connectedCameraDevice.StartCapture();

            ////ExecuteCommand(string.Format("--file_format dng -o {0} --iso {1} --shutter_speed {2}", fileName + ".dng", Iso, Duration));
            ////pktriggercord-cli --file_format dng -o c:\temp\test.dng -i 400 -t 1
            //Logger.WriteTraceMessage("--file_format dng -o " + fileName + ".dng -i " + Iso + " -t " + Duration);
            //ExecuteCommand(string.Format("--file_format dng -o {0} -i {1} -t {2}", fileName + ".dng", Iso, Duration));

        }

        private string _fileNameWaiting;


        private void MarkWaitingForExposure(double Duration, string fileName)
        {
            _exposureStartTime = DateTime.Now;
            _lastDuration = Duration;
            _waitingForImage = true;
            _fileNameWaiting = fileName;
        }

        FileSystemWatcher watcher;

        private void watch()
        {
            if (!Directory.Exists(StorePath))
            {
                Directory.CreateDirectory(StorePath);
            }

            watcher = new FileSystemWatcher();
            watcher.Path = StorePath;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.dng";
            watcher.Changed += new FileSystemEventHandler(OnChanged);
            watcher.EnableRaisingEvents = true;

            Logger.WriteTraceMessage("watch " + StorePath);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            var fileName = e.FullPath;

            Logger.WriteTraceMessage("onchanged " + fileName);

            var destinationFilePath = Path.ChangeExtension(Path.Combine(StorePath, Path.Combine(StorePath, _fileNameWaiting)), ".dng");

            Logger.WriteTraceMessage("onchanged dest " + destinationFilePath);

            File.Copy(fileName, destinationFilePath);
            File.Delete(fileName);
            if (ImageReady != null)
            {
                ImageReady(this, new ImageReadyEventArgs(destinationFilePath));
            }
            watcher.Changed -= OnChanged;
            watcher.EnableRaisingEvents = false;
            watcher = null;

            if ((File.Exists(destinationFilePath)) && (SaveFile == false))
            {
                File.Delete(destinationFilePath);
            }

        }

        private string GetAppPath()
        {
            string AppPath;
            AppPath = Assembly.GetExecutingAssembly().Location;
            AppPath = Path.GetDirectoryName(AppPath);

            return AppPath;
        }

        private Dictionary<string, string> ParseStatus(string status)
        {
            var result = new Dictionary<string, string>();

            using (StringReader sr = new StringReader(status))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var parts = line.Split(':').Select(p => p.Trim()).ToList();
                    if (parts.Count == 2)
                    {
                        result.Add(parts[0], parts[1]);
                    }
                }
            }

            return result;
        }

        public string ExecuteCommand(string args)
        {
            Logger.WriteTraceMessage("ExecuteCommand(), args = '" + args + "'");

            string exeDir = Path.Combine(GetAppPath(), "pktriggercord", "pktriggercord-cli.exe");
            ProcessStartInfo procStartInfo = new ProcessStartInfo();

            procStartInfo.FileName = exeDir;
            procStartInfo.Arguments = args + " --timeout 10";
            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;
            Logger.WriteTraceMessage("about to start process with command = '" + procStartInfo.FileName + " " + procStartInfo.Arguments + "'");

            string result = string.Empty;
            using (Process process = new Process())
            {
                process.StartInfo = procStartInfo;
                process.Start();
                process.WaitForExit();

                result = process.StandardOutput.ReadToEnd();
                Logger.WriteTraceMessage("result of command = '" + result + "'");
            }
            //result = "pktriggercord-cli: K-5IIs Connected...";
            return result;
        }

        private void CallExposureFailed(string message, string stackTrace = null)
        {
            _waitingForImage = false;
            ExposureFailed?.Invoke(this, new ExposureFailedEventArgs(message, stackTrace));
        }

        public void StopExposure()
        {
            AbortExposure();
        }
    }
}
