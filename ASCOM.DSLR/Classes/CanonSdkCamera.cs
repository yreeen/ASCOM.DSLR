﻿using ASCOM.DSLR.Enums;
using ASCOM.DSLR.Interfaces;
using EDSDKLib.API.Base;
using EOSDigital.API;
using EOSDigital.SDK;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ASCOM.DSLR.Classes
{
    public class CanonSdkCamera : BaseCanonCamera, IDslrCamera, IDisposable
    {
        public CanonSdkCamera()
        {
            APIHandler = new CanonAPI();
            APIHandler.CameraAdded += APIHandler_CameraAdded;
            ErrorHandler.SevereErrorHappened += ErrorHandler_SevereErrorHappened;
            ErrorHandler.NonSevereErrorHappened += ErrorHandler_NonSevereErrorHappened;
        }

        CanonAPI APIHandler;

        EOSDigital.API.Camera _mainCamera;
        EOSDigital.API.Camera MainCamera
        {
            get
            {
                if (_mainCamera == null)
                {
                    ScanCameras();
                }
                return _mainCamera;
            }
        }

        List<EOSDigital.API.Camera> CamList;

        private void CloseCamera()
        {
            CloseSession();
            _mainCamera?.Dispose();
            APIHandler?.Dispose();
        }

        private void APIHandler_CameraAdded(CanonAPI sender)
        {
        }

        public override CameraModel ScanCameras()
        {
            CamList = APIHandler.GetCameraList();
            if (!CamList.Any())
            {
                throw new NotConnectedException(ErrorMessages.NotConnected);
            }
            _mainCamera = CamList.First();
            var cameraModel = GetCameraModel(_mainCamera.DeviceName);
            return cameraModel;
        }


        private void MainCamera_StateChanged(EOSDigital.API.Camera sender, StateEventID eventID, int parameter)
        {

        }

        private void MainCamera_ProgressChanged(object sender, int progress)
        {

        }

        public bool SupportsViewView { get { return true; } }
        

        private DateTime _startTime;

        private void MainCamera_DownloadReady(EOSDigital.API.Camera sender, DownloadInfo Info)
        {           
            if (!Directory.Exists(StorePath))
            {
                Directory.CreateDirectory(StorePath);
            }
            sender.DownloadFile(Info, StorePath);

            string downloadedFilePath = Path.Combine(StorePath, Info.FileName);
            SensorTemperature = GetSensorTemperature(downloadedFilePath);

            string newFilePath = RenameFile(downloadedFilePath, _duration, _startTime);
            ImageReady?.Invoke(this, new ImageReadyEventArgs(newFilePath));
        }

       

        private void ErrorHandler_NonSevereErrorHappened(object sender, ErrorCode ex)
        {
            ExposureFailed?.Invoke(this, new ExposureFailedEventArgs(ErrorMessages.CameraError, ex.ToString()));
        }

        private void ErrorHandler_SevereErrorHappened(object sender, Exception ex)
        {
            ExposureFailed?.Invoke(this, new ExposureFailedEventArgs(ex.Message, ex.StackTrace));
        }

        private void CloseSession()
        {
            if (_mainCamera != null && _mainCamera.SessionOpen)
            {
                _mainCamera.ProgressChanged -= MainCamera_ProgressChanged;
                _mainCamera.StateChanged -= MainCamera_StateChanged;
                _mainCamera.DownloadReady -= MainCamera_DownloadReady;
                MainCamera.LiveViewUpdated -= MainCamera_LiveViewUpdated;
                _mainCamera.CloseSession();
            }
        }

        private void OpenSession()
        {
            ScanCameras();
            if (!MainCamera.SessionOpen)
            {
                MainCamera.OpenSession();
                MainCamera.ProgressChanged += MainCamera_ProgressChanged;
                MainCamera.StateChanged += MainCamera_StateChanged;
                MainCamera.DownloadReady += MainCamera_DownloadReady;
                MainCamera.LiveViewUpdated += MainCamera_LiveViewUpdated;                

                TvList = MainCamera.GetSettingsList(PropertyID.Tv);
                ISOList = MainCamera.GetSettingsList(PropertyID.ISO);
            }
        }

        private void MainCamera_LiveViewUpdated(EOSDigital.API.Camera sender, Stream img)
        {
            if (_lvInitialized)
            {
                var Evf_Bmp = new Bitmap(img);
                if (LvFrameHeight == 0 || LvFrameWidth == 0)
                {
                    LvFrameWidth = Evf_Bmp.Width;
                    LvFrameHeight = Evf_Bmp.Height;
                }

                if (LiveViewImageReady != null && _lvCapture)
                {
                    LiveViewImageReady(this, new LiveViewImageReadyEventArgs(Evf_Bmp));
                    _lvCapture = false;

                }
            }
        }

        public string Model
        {
            get
            {
                return MainCamera.DeviceName;
            }
        }        

        public ConnectionMethod IntegrationApi => ConnectionMethod.CanonSdk;

      

        public event EventHandler<ImageReadyEventArgs> ImageReady;
        public event EventHandler<ExposureFailedEventArgs> ExposureFailed;
        public event EventHandler<LiveViewImageReadyEventArgs> LiveViewImageReady;

        public void AbortExposure()
        {
            _canceledFlag.IsCanceled = true;
        }

        private void InitSettings()
        {
            MainCamera.SetSetting(PropertyID.SaveTo, (int)SaveTo.Host);
            MainCamera.SetCapacity(1024, int.MaxValue);

            switch (ImageFormat)
            {
                case ImageFormat.RAW:
                    MainCamera.SetSetting(PropertyID.ImageQuality, (int)ImageQuality.RAW);
                    break;
                case ImageFormat.JPEG:
                    MainCamera.SetSetting(PropertyID.ImageQuality, (int)ImageQuality.LargeJpegFine);
                    break;
            }

            CameraValue selectedIsoValue = GetSelectedIsoValue();
            MainCamera.SetSetting(PropertyID.ISO, selectedIsoValue.IntValue);
        }

        private CameraValue GetSelectedIsoValue()
        {
            var selectedIsoValue = ISOList.SingleOrDefault(v => v.DoubleValue == Iso && v.DoubleValue > 0);
            if (selectedIsoValue == null)
            {
                var nearest = ISOValues.Values.Where(v => v.DoubleValue < short.MaxValue && v.DoubleValue > 0)
                    .Select(v => new { value = v, difference = Math.Abs(v.DoubleValue - Iso) }).OrderBy(d => d.difference).First().value;

                selectedIsoValue = nearest;
            }

            var isoValue = ISOValues.GetValue((double)Iso);

            return selectedIsoValue;
        }

        private CameraValue GetSelectedTv(double Duration)
        {
            var nearestTv = TvList.Select(t => new { Tv = t, delta = Math.Abs(t.DoubleValue - Duration) }).OrderBy(d => d.delta);
            var tvCameraValue = nearestTv.First().Tv;
            return tvCameraValue;
        }

        private CanceledFlag _canceledFlag = new CanceledFlag();

        private double _duration;

        private bool _lvCapture = false;

        public void StartExposure(double Duration, bool Light)
        {
            if (!IsLiveViewMode)
            {
                InitSettings();

                _duration = Duration;
                _startTime = DateTime.Now;
                _canceledFlag.IsCanceled = false;

                if (Duration >= 1)
                {
                    MainCamera.SetSetting(PropertyID.Tv, TvValues.GetValue("Bulb").IntValue);
                    MainCamera.TakePhotoBulbAsync((int)(Duration * 1000), _canceledFlag);
                }
                else
                {
                    CameraValue tvCameraValue = GetSelectedTv(Duration);
                    MainCamera.SetSetting(PropertyID.Tv, tvCameraValue.IntValue);
                    MainCamera.TakePhoto();
                }
            }
            else
            {
                _lvCapture = true;
            }
        }

        public void StopExposure()
        {
            AbortExposure();
        }

        public void Dispose()
        {
            CloseCamera();
        }

        private bool _lvInitialized;
        public void ConnectCamera()
        {
            OpenSession();
            if (IsLiveViewMode)
            {
                LvFrameHeight = 0;
                LvFrameWidth = 0;

                MainCamera.StartLiveView();
                Thread.Sleep(1000);
                MainCamera.SetSetting(PropertyID.Evf_Zoom, (UInt32)LiveViewZoom);
                Thread.Sleep(1000);
                _lvInitialized = true;

                while (LvFrameHeight == 0 || LvFrameWidth == 0)
                {
                    Thread.Sleep(500);
                }
            }
            
        }

        public void DisconnectCamera()
        {
            if (IsLiveViewMode && MainCamera.SessionOpen)
            {
                MainCamera.StopLiveView();
            }
            CloseSession();
        }
    }
}
