using DPUruNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Checador
{
    public partial class EnrollmentControl : Form
    {
        public MainWindow _sender;

        private DPCtlUruNet.EnrollmentControl _enrollmentControl;
        public EnrollmentControl()
        {
            InitializeComponent();
        }
        
        private Reader currentReader;
        public Reader CurrentReader
        {
            get { return currentReader; }
            set
            {
                currentReader = value;
                SendMessage(Action.UpdateReaderState, value);
            }
        }

        // Selección de dispositivo lector
        private ReaderCollection _readers;
        private void LoadScanners()
        {
            cboReaders.Text = string.Empty;
            cboReaders.Items.Clear();
            cboReaders.SelectedIndex = -1;

            try
            {
                _readers = ReaderCollection.GetReaders();

                foreach (Reader Reader in _readers)
                {
                    cboReaders.Items.Add(Reader.Description.Name);
                }

                if (cboReaders.Items.Count > 0)
                {
                    cboReaders.SelectedIndex = 0;
                    //btnCaps.Enabled = true;
                    //btnSelect.Enabled = true;
                }
                else
                {
                    //btnSelect.Enabled = false;
                    //btnCaps.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                //message box:
                String text = ex.Message;
                text += "\r\n\r\nPlease check if DigitalPersona service has been started";
                String caption = "Cannot access readers";
                MessageBox.Show(text, caption);
            }
        }

        private const int PROBABILITY_ONE = 0x7fffffff;
        List<Fmd> preenrollmentFmds;
        DataResult<Fmd> resultEnrollment;
        private Fmd firstFinger;
        int count = 0;
                

        /// <summary>
        /// Open a device and check result for errors.
        /// </summary>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool OpenReader()
        {
            using (Tracer tracer = new Tracer("EnrollmentControl::OpenReader"))
            {
                reset = false;
                Constants.ResultCode result = Constants.ResultCode.DP_DEVICE_FAILURE;

                // Open reader
                result = currentReader.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

                if (result != Constants.ResultCode.DP_SUCCESS)
                {
                    MessageBox.Show("Error:  " + result);
                    reset = true;
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Check the device status before starting capture.
        /// </summary>
        /// <returns></returns>
        public void GetStatus()
        {
            using (Tracer tracer = new Tracer("EnrollmentControl::GetStatus"))
            {
                Constants.ResultCode result = currentReader.GetStatus();

                if ((result != Constants.ResultCode.DP_SUCCESS))
                {
                    reset = true;
                    throw new Exception("" + result);
                }

                if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_BUSY))
                {
                    Thread.Sleep(50);
                }
                else if ((currentReader.Status.Status == Constants.ReaderStatuses.DP_STATUS_NEED_CALIBRATION))
                {
                    currentReader.Calibrate();
                }
                else if ((currentReader.Status.Status != Constants.ReaderStatuses.DP_STATUS_READY))
                {
                    throw new Exception("Reader Status - " + currentReader.Status.Status);
                }
            }
        }

        /// <summary>
        /// Function to capture a finger. Always get status first and calibrate or wait if necessary.  Always check status and capture errors.
        /// </summary>
        /// <param name="fid"></param>
        /// <returns></returns>
        public bool CaptureFingerAsync()
        {
            using (Tracer tracer = new Tracer("EnrollmentControl::CaptureFingerAsync"))
            {
                try
                {
                    GetStatus();

                    Constants.ResultCode captureResult = currentReader.CaptureAsync(Constants.Formats.Fid.ANSI, Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT, currentReader.Capabilities.Resolutions[0]);
                    if (captureResult != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception("" + captureResult);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error:  " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Hookup capture handler and start capture.
        /// </summary>
        /// <param name="OnCaptured">Delegate to hookup as handler of the On_Captured event</param>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool StartCaptureAsync(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("EnrollmentControl::StartCaptureAsync"))
            {
                // Activate capture handler
                currentReader.On_Captured += new Reader.CaptureCallback(OnCaptured);

                // Call capture
                if (!CaptureFingerAsync())
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Check quality of the resulting capture.
        /// </summary>
        public bool CheckCaptureResult(CaptureResult captureResult)
        {
            using (Tracer tracer = new Tracer("EnrollmentControl::CheckCaptureResult"))
            {
                if (captureResult.Data == null || captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    if (captureResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                    {
                        reset = true;
                        throw new Exception(captureResult.ResultCode.ToString());
                    }

                    // Send message if quality shows fake finger
                    if ((captureResult.Quality != Constants.CaptureQuality.DP_QUALITY_CANCELED))
                    {
                        throw new Exception("Quality - " + captureResult.Quality);
                    }
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Create a bitmap from raw data in row/column format.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public Bitmap CreateBitmap(byte[] bytes, int width, int height)
        {
            byte[] rgbBytes = new byte[bytes.Length * 3];

            for (int i = 0; i <= bytes.Length - 1; i++)
            {
                rgbBytes[(i * 3)] = bytes[i];
                rgbBytes[(i * 3) + 1] = bytes[i];
                rgbBytes[(i * 3) + 2] = bytes[i];
            }
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

            for (int i = 0; i <= bmp.Height - 1; i++)
            {
                IntPtr p = new IntPtr(data.Scan0.ToInt64() + data.Stride * i);
                System.Runtime.InteropServices.Marshal.Copy(rgbBytes, i * bmp.Width * 3, p, bmp.Width * 3);
            }

            bmp.UnlockBits(data);

            return bmp;
        }

        /// <summary>
        /// Handler for when a fingerprint is captured.
        /// </summary>
        /// <param name="captureResult">contains info and data on the fingerprint capture</param>
        private void OnCaptured(CaptureResult captureResult)
        {
            try
            {
                //this.Invoke( () =>
                //{
                //    btnCancel.Enabled = true;
                //});
                // Check capture quality and throw an error if bad.
                if (!CheckCaptureResult(captureResult)) return;

                count++;

                DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);

                SendMessage(Action.SendMessage, "La huella fue capturada.  \r\nCount:  " + (count));
                
                // Create bitmap
                foreach (Fid.Fiv fiv in captureResult.Data.Views)
                {
                    SendMessage(Action.SendBitmap, CreateBitmap(fiv.RawImage, fiv.Width, fiv.Height));
                }

                if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    Reset = true;
                    throw new Exception(resultConversion.ResultCode.ToString());
                }

                preenrollmentFmds.Add(resultConversion.Data);

                if (count >= 4)
                {
                    DataResult<Fmd> resultEnrollment = DPUruNet.Enrollment.CreateEnrollmentFmd(Constants.Formats.Fmd.ANSI, preenrollmentFmds);

                    if (resultEnrollment.ResultCode == Constants.ResultCode.DP_SUCCESS)
                    {
                        firstFinger = resultEnrollment.Data;
                        if (_sender.Fmds.Count > 0)
                            _sender.Fmds.Clear();
                        _sender.Fmds.Add(1,resultEnrollment.Data);
                        SendMessage(Action.SendMessage, "El registro de la huella fue creado.");
                        //resetea intentos
                        preenrollmentFmds.Clear();
                        count = 0;
                        SendMessage(Action.SendBox, "La Huella ya se guardo, puedes salir de la ventana o reemplazar la huella.");
                        
                        //btnCancel.Enabled = false;
                        
                        return;
                    }
                    else if (resultEnrollment.ResultCode == Constants.ResultCode.DP_ENROLLMENT_INVALID_SET)
                    {
                        SendMessage(Action.SendMessage, "El registro de la huella no fue exitoso. Vuela a intentar.");
                        SendMessage(Action.SendMessage, "Coloque su dedo en el lector.");
                        preenrollmentFmds.Clear();
                        count = 0;
                        return;
                    }
                    
                }

                SendMessage(Action.SendMessage, "Coloque una vez mas el mismo dedo en el lector.");
            }
            catch (Exception ex)
            {
                // Send error message, then close form
                SendMessage(Action.SendMessage, "Error:  " + ex.Message);
                count = 0;
            }
        }

        /// <summary>
        /// Reset the UI causing the user to reselect a reader.
        /// </summary>
        public bool Reset
        {
            get { return reset; }
            set { reset = value; }
        }
        private bool reset;

        // Actions para imprimir.
        private enum Action
        {
            UpdateReaderState,
            SendBitmap,
            SendMessage,
            SendBox
        }

        private delegate void SendMessageCallback(Action state, object payload);
        //Funcion generica que imprime mensajes, dependiendo de la acción
        private void SendMessage(Action action, object payload)
        {
            try
            {
                if (this.pbFingerprint.InvokeRequired)
                {
                    SendMessageCallback d = new SendMessageCallback(SendMessage);
                    this.Invoke(d, new object[] { action, payload });
                }
                else
                {
                    switch (action)
                    {
                        case Action.SendMessage:
                            // Imprime en textbox
                            //MessageBox.Show((string)payload);
                            txtMessage.Text += (string)payload + "\r\n\r\n";
                            txtMessage.SelectionStart = txtMessage.TextLength;
                            txtMessage.ScrollToCaret();
                            break;
                        case Action.SendBitmap:
                            // Imprime la imagen de la huella
                            pbFingerprint.Image = (Bitmap)payload;
                            pbFingerprint.Refresh();
                            break;
                        case Action.SendBox:
                            // Imprime la imagen de la huella
                            MessageBox.Show((string)payload, "Mensaje");
                            break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void EnrollmentControl_Load(object sender, EventArgs e)
        {
            // Reset variables
            LoadScanners();
            firstFinger = null;
            resultEnrollment = null;
            preenrollmentFmds = new List<Fmd>();
            pbFingerprint.Image = null;

            // Establece el dispositivo a usar.
            if (CurrentReader != null)
            {
                CurrentReader.Dispose();
                CurrentReader = null;
            }
            CurrentReader = _readers[cboReaders.SelectedIndex];

            // Revisa si existen errores en el lector
            if (!OpenReader())
            {
                //this.Close();
            }

            // Escucha por la captura de huella. 
            if (!StartCaptureAsync(this.OnCaptured))
            {
                //this.Close();
            }
        }

        /// <summary>
        /// Cancel the capture and then close the reader.
        /// </summary>
        /// <param name="OnCaptured">Delegate to unhook as handler of the On_Captured event </param>
        public void CancelCaptureAndCloseReader(Reader.CaptureCallback OnCaptured)
        {
            if (currentReader != null)
            {
                currentReader.CancelCapture();

                // Dispose of reader handle and unhook reader events.
                currentReader.Dispose();

                if (reset)
                {
                    CurrentReader = null;
                }
            }
        }

        /// <summary>
        /// Close window.
        /// </summary>
        private void EnrollmentControl_FormClosed(object sender, FormClosedEventArgs e)
        {
            CancelCaptureAndCloseReader(this.OnCaptured);
        }

        /// <summary>
        /// Cancel enrollment when window is closed.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks></remarks>
        private void btnCancel_Click(object sender, EventArgs e)
        {
            MessageBoxButtons buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            result = MessageBox.Show("¿Seguro que quieres cancelar el registro de huella?", "¿Estás seguro?", buttons, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

            if (result == System.Windows.Forms.DialogResult.Yes)
            {
                _enrollmentControl.Cancel();
            }
        }

        /// <summary>
        /// Close window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks></remarks>
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
