using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ASCOM.Utilities;
using ASCOM.DSLR;
using ASCOM.DeviceInterface;
using ASCOM.DSLR.Classes;
using ASCOM.DSLR.Enums;
using System.IO;
using System.Linq;
using EOSDigital.API;
using System.Threading;
using System.IO.Ports;
using Logging;

namespace ASCOM.DSLR
{
    [ComVisible(false)]					
    public partial class SetupDialogForm : Form
    {
        public SetupDialogForm(CameraSettings settings)
        {
            Settings = settings;
            InitializeComponent();
            InitUI();
        }

        private void cmdOK_Click(object sender, EventArgs e) // OK button event handler
        {
            Settings.TraceLog = chkTrace.Checked;
            Settings.CameraMode = (CameraMode)cbImageMode.SelectedItem;

            Settings.SaveFile = chkSaveFile.Checked;

            Settings.IntegrationApi = (ConnectionMethod)cbIntegrationApi.SelectedItem;

            if (Directory.Exists(tbSavePath.Text))
            {
                Settings.StorePath = tbSavePath.Text;
            }
            Settings.Iso = (short)cbIso.SelectedValue;

            if (!string.IsNullOrEmpty(tbBackyardEosPort.Text))
            {
                Settings.BackyardEosPort = int.Parse(tbBackyardEosPort.Text);
            }
            Settings.EnableBinning = chkEnableBin.Checked;
            Settings.BinningMode = (BinningMode)cbBinningMode.SelectedItem;

            Settings.UseExternalShutter = chkUseExternalShutter.Checked;
            Settings.ExternalShutterPortName = cbShutterPort.SelectedItem as string;

            Settings.LiveViewCaptureMode = chkEnableLiveView.Checked;
            Settings.LiveViewZoom = (LiveViewZoom)cbLiveViewZoom.SelectedItem;

            Settings.LogLevel = (DebugLogLevels)cbTraceLevel.SelectedItem;

            Settings.maxADU = Convert.ToInt32(txtMAXADU.Text);

            Settings.maxADUOverride = chkMAXADU.Checked;

            if ((DebugLogLevels)cbTraceLevel.SelectedItem == DebugLogLevels.Trace)
                Logger.SetLogLevelTrace();

            if ((DebugLogLevels)cbTraceLevel.SelectedItem == DebugLogLevels.Debug)
                Logger.SetLogLevelDebug();

            if ((DebugLogLevels)cbTraceLevel.SelectedItem == DebugLogLevels.Info)
                Logger.SetLogLevelInfo();

            if ((DebugLogLevels)cbTraceLevel.SelectedItem == DebugLogLevels.Warn)
                Logger.SetLogLevelWarn();

            if ((DebugLogLevels)cbTraceLevel.SelectedItem == DebugLogLevels.Error)
                Logger.SetLogLevelError();

            if ((DebugLogLevels)cbTraceLevel.SelectedItem == DebugLogLevels.Fatal)
                Logger.SetLogLevelFatal();

        }

        private void cmdCancel_Click(object sender, EventArgs e) // Cancel button event handler
        {
            Close();
        }

        private void BrowseToAscom(object sender, EventArgs e) // Click on ASCOM logo event handler
        {
            try
            {
                System.Diagnostics.Process.Start("http://ascom-standards.org/");
            }
            catch (System.ComponentModel.Win32Exception noBrowser)
            {
                if (noBrowser.ErrorCode == -2147467259)
                    MessageBox.Show(noBrowser.Message);
            }
            catch (System.Exception other)
            {
                MessageBox.Show(other.Message);
            }
        }

        public CameraSettings Settings { get; private set; }

        private void SetSelectedItem(ComboBox comboBox, object selectedItem)
        {
            if (comboBox.Items.Contains(selectedItem))
            {
                comboBox.SelectedItem = selectedItem;
            }
        }

        private void InitUI()
        {
            chkTrace.Checked = Settings.TraceLog;

            chkSaveFile.Checked = Settings.SaveFile;

            cbImageMode.Items.Clear();
            cbImageMode.Items.Add(CameraMode.RGGB);
            cbImageMode.Items.Add(CameraMode.Color16);
            cbImageMode.Items.Add(CameraMode.ColorJpg);
            SetSelectedItem(cbImageMode, Settings.CameraMode);

            chkEnableBin.Checked = Settings.EnableBinning;
            
            cbIntegrationApi.Items.Add(ConnectionMethod.CanonSdk);
            cbIntegrationApi.Items.Add(ConnectionMethod.BackyardEOS);
            cbIntegrationApi.Items.Add(ConnectionMethod.Nikon);
            cbIntegrationApi.Items.Add(ConnectionMethod.Pentax);
            cbIntegrationApi.Items.Add(ConnectionMethod.NikonLegacy);
            cbIntegrationApi.Items.Add(ConnectionMethod.PentaxSDK);
            SetSelectedItem(cbIntegrationApi, Settings.IntegrationApi);
            

            cbBinningMode.Items.Add(BinningMode.Sum);
            cbBinningMode.Items.Add(BinningMode.Median);
            SetSelectedItem(cbBinningMode, Settings.BinningMode);


            var isoValues = ISOValues.Values.Where(v => v.DoubleValue <= short.MaxValue && v.DoubleValue>0).Select(v => (short)v.DoubleValue);
            cbIso.DisplayMember = "display";
            cbIso.ValueMember = "value";
            cbIso.DataSource =  isoValues.Select(v => new { value = v, display = v.ToString() }).ToArray();
            cbIso.SelectedValue = Settings.Iso;


            tbSavePath.Text = Settings.StorePath;

            txtMAXADU.Text = Convert.ToString(Settings.maxADU);

            chkMAXADU.Checked = Settings.maxADUOverride;

            tbBackyardEosPort.Text = Settings.BackyardEosPort.ToString();

            chkUseExternalShutter.Checked = Settings.UseExternalShutter;
            
            foreach (var port in SerialPort.GetPortNames())
            {
                cbShutterPort.Items.Add(port);
            }
            if (!string.IsNullOrEmpty(Settings.ExternalShutterPortName))
            {
                cbShutterPort.SelectedIndex = cbShutterPort.FindStringExact(Settings.ExternalShutterPortName);
            }

            cbLiveViewZoom.Items.Add(LiveViewZoom.Fit);
            cbLiveViewZoom.Items.Add(LiveViewZoom.x5);
            cbLiveViewZoom.Items.Add(LiveViewZoom.x10);

            chkEnableLiveView.Checked = Settings.LiveViewCaptureMode;
            SetSelectedItem(cbLiveViewZoom, Settings.LiveViewZoom);

            cbTraceLevel.Items.Add(DebugLogLevels.Trace);
            cbTraceLevel.Items.Add(DebugLogLevels.Debug);
            cbTraceLevel.Items.Add(DebugLogLevels.Info);
            cbTraceLevel.Items.Add(DebugLogLevels.Warn);
            cbTraceLevel.Items.Add(DebugLogLevels.Error);
            cbTraceLevel.Items.Add(DebugLogLevels.Fatal);
            SetSelectedItem(cbTraceLevel, Settings.LogLevel);

            UpdateUiState();

        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(tbSavePath.Text))
            {
                folderBrowserDialog.SelectedPath = tbSavePath.Text;
            }

            var thread = new Thread(new ParameterizedThreadStart(param => 
            {
                if (folderBrowserDialog.ShowDialog(this) == DialogResult.OK)
                {
                    Invoke((Action)delegate { tbSavePath.Text = folderBrowserDialog.SelectedPath; });
                }
            }));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        private void tbBackyardEosPort_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void chkEnableBin_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUiState();
        }

        private void EnableBinChanged()
        {
            bool isLv = IsLiveView();
            if (chkEnableBin.Checked)
            {
                SetSelectedItem(cbImageMode, CameraMode.RGGB);

                cbImageMode.Visible = false;
                lbImageMode.Visible = false;
                cbBinningMode.Enabled = true && !isLv;
            }
            else
            {
                cbImageMode.Visible = true && !isLv;
                lbImageMode.Visible = true && !isLv;
                cbBinningMode.Enabled = false;
            }
        }

        private void lblBinningMode_Click(object sender, EventArgs e)
        {

        }

        private void cbBinningMode_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void cbIntegrationApi_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateUiState();
        }

        private void UpdateUiState()
        {
            ConnectionMethodChanged();
            LiveViewModeChagned();
            UseExternalShutterChanged();
            EnableBinChanged();
        }

        private void ConnectionMethodChanged()
        {
            bool isBEOS = IsBeos();
            tbBackyardEosPort.Visible = isBEOS;
            lblBackyardEosPort.Visible = isBEOS;
            bool isNikonSDK = IsNikonSDK();
            bool isDigiCamControl = IsDigiCamControl();
            chkUseExternalShutter.Visible = isDigiCamControl;
            cbShutterPort.Visible = isDigiCamControl;
            bool isCanon = IsCanon();
            bool isPentax = IsPentax();
            bool isPentaxSDK = IsPentaxSDK();
            if (isCanon || isNikonSDK)
            {
                chkEnableLiveView.Visible = true;
                lblLiveViewZoom.Visible = true;
                cbLiveViewZoom.Visible = true;
            }
            else
            {
                chkEnableLiveView.Checked = false;
                chkEnableLiveView.Visible = false;
                lblLiveViewZoom.Visible = false;
                cbLiveViewZoom.Visible = false;
            }
            if(isPentaxSDK)
            {
                chkEnableLiveView.Visible = true;
            }
        }

        private bool IsCanon()
        {
            return cbIntegrationApi.SelectedItem != null && (ConnectionMethod)cbIntegrationApi.SelectedItem == ConnectionMethod.CanonSdk;
        }

        private bool IsPentax()
        {
            return cbIntegrationApi.SelectedItem != null && (ConnectionMethod)cbIntegrationApi.SelectedItem == ConnectionMethod.Pentax;
        }

        private bool IsDigiCamControl()
        {
            return cbIntegrationApi.SelectedItem !=null &&  (ConnectionMethod)cbIntegrationApi.SelectedItem == ConnectionMethod.NikonLegacy;
        }

        private bool IsBeos()
        {
            return cbIntegrationApi.SelectedItem != null && (ConnectionMethod)cbIntegrationApi.SelectedItem == ConnectionMethod.BackyardEOS;
        }

        private bool IsNikonSDK()
        {
            return cbIntegrationApi.SelectedItem != null && (ConnectionMethod)cbIntegrationApi.SelectedItem == ConnectionMethod.Nikon;
        }

        private bool IsPentaxSDK()
        {
            return cbIntegrationApi.SelectedItem != null && (ConnectionMethod)cbIntegrationApi.SelectedItem == ConnectionMethod.PentaxSDK;
        }

        private void chkUseExternalShutter_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUiState();
        }

        private void UseExternalShutterChanged()
        {
            cbShutterPort.Enabled = chkUseExternalShutter.Checked;
        }

        private void cbImageMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void LiveViewModeChagned()
        {
            bool isLiveView = IsLiveView();

            chkUseExternalShutter.Visible = !isLiveView;
            cbShutterPort.Visible = !isLiveView;

            chkEnableBin.Visible = !isLiveView;
            cbBinningMode.Visible = !isLiveView;

            lblSavePhotosTo.Visible = !isLiveView;
            tbSavePath.Visible = !isLiveView;
            btnBrowse.Visible = !isLiveView;

            lblIso.Visible = !isLiveView;
            cbIso.Visible = !isLiveView;

            lblLiveViewZoom.Visible = isLiveView;
            cbLiveViewZoom.Visible = isLiveView;

            cbImageMode.Visible = !isLiveView;
            lbImageMode.Visible = !isLiveView;
        }

        private bool IsLiveView()
        {
            return chkEnableLiveView.Checked;
        }

        private void chkEnableLiveView_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUiState();
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            var aboutForm = new About();
            aboutForm.ShowDialog(this);
        }

        private void SetupDialogForm_Load(object sender, EventArgs e)
        {

        }

        private void chkSaveFile_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cbIso_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void cbTraceLevel_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void chkTrace_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void chxMAXADU_CheckedChanged(object sender, EventArgs e)
        {
            if (chkMAXADU.Checked)
            {
                txtMAXADU.Enabled = true;
            }
            else
            {
                txtMAXADU.Enabled = false;
            }
        }
    }
}