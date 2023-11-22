using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Checador.Services;
using DPCtlUruNet;
using DPUruNet;
using DPXUru;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using System.Threading;
using Checador.Models;
using System.Security.Policy;

namespace Checador
{
    /// <summary>
    /// Lógica de interacción para Check.xaml
    /// </summary>
    public partial class Check : Window
    {
        public MainWindow _sender;
        private const int DPFJ_PROBABILITY_ONE = 0x7fffffff;
        private Fmd huella_capturada;
        private int count;
        private DPCtlUruNet.IdentificationControl identificationControl;
        public Check()
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

        /// <summary>
        /// Open a device and check result for errors.
        /// </summary>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool OpenReader()
        {
            using (Tracer tracer = new Tracer("Check::OpenReader"))
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
                this.Dispatcher.Invoke(() =>
                {
                    switch (action)
                    {
                        case Action.SendMessage:
                            // Imprime en textbox
                            //MessageBox.Show((string)payload);
                            txtMessage.Text += (string)payload + "\r\n\r\n";
                            //txtMessage.SelectionStart = txtMessage.SelectionLength;
                            txtMessage.ScrollToEnd();
                            break;
                        case Action.SendBox:
                            // Imprime la imagen de la huella
                            MessageBox.Show((string)payload, "Mensaje");
                            break;
                    }
                });
            }
            catch (Exception)
            {
            }
        }

        public void ResetUI()
        {
            lblNombres.Content = "¡Hola!";
            lblApellidos.Content = "";
            lblNumero.Content = "Coloca tu dedo en el lector.";

            BitmapImage foto = new BitmapImage();
            foto.BeginInit();
            foto.UriSource = new Uri("Resources/noimagen.jpg");
            foto.EndInit();
            foto.Freeze();

            imgFoto.Source = foto;
        }

        public void Desplegar(Empleado empleado)
        {
            string url = "";

            lblNombres.Content = empleado.Nombres;
            lblApellidos.Content = empleado.Apellidos;
            lblNumero.Content = empleado.Numero;
            if (empleado.Foto != "" && empleado.Foto != null)
            {
                url = "C:/Checador/" + empleado.Foto;
            }
            else
                url = "Resources/noimagen.jpg";

            BitmapImage foto = new BitmapImage();
            foto.BeginInit();
            foto.UriSource = new Uri(url);
            foto.EndInit();
            foto.Freeze();

            imgFoto.Source = foto;
        }

        //private void identificationControl_OnIdentify(DPCtlUruNet.IdentificationControl IdentificationControl, IdentifyResult IdentificationResult)
        //{
        //    if (IdentificationResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
        //    {
        //        if (IdentificationResult.Indexes == null)
        //        {
        //            if (IdentificationResult.ResultCode == Constants.ResultCode.DP_INVALID_PARAMETER)
        //            {
        //                MessageBox.Show("Cuidado: Huella erronea detectada.");
        //            }
        //            else if (IdentificationResult.ResultCode == Constants.ResultCode.DP_NO_DATA)
        //            {
        //                MessageBox.Show("Cuidado: Huella no detectada.");
        //            }
        //            else
        //            {
        //                if (CurrentReader != null)
        //                {
        //                    CurrentReader.Dispose();
        //                    CurrentReader = null;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (CurrentReader != null)
        //            {
        //                CurrentReader.Dispose();
        //                CurrentReader = null;
        //            }

        //            MessageBox.Show("Error:  " + IdentificationResult.ResultCode);
        //        }
        //    }
        //    else
        //    {
        //        CurrentReader = IdentificationControl.Reader;
        //        txtMessage.Text = txtMessage.Text + "Encontradas:  " + (IdentificationResult.Indexes.Length.Equals(0) ? "cero " : "una ") + "coincidencia.  Intenta con otro dedo.\r\n\r\n";
        //    }

        //    txtMessage.SelectionStart = txtMessage.SelectionLength;
        //    txtMessage.ScrollToEnd();
        //}

        /// <summary>
        /// Check the device status before starting capture.
        /// </summary>
        /// <returns></returns>
        public void GetStatus()
        {
            using (Tracer tracer = new Tracer("Check::GetStatus"))
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
                    throw new Exception("Estatus del lector - " + currentReader.Status.Status);
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
            using (Tracer tracer = new Tracer("Check::CaptureFingerAsync"))
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
        /// Check quality of the resulting capture.
        /// </summary>
        public bool CheckCaptureResult(CaptureResult captureResult)
        {
            using (Tracer tracer = new Tracer("Check::CheckCaptureResult"))
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
                        throw new Exception("Calidad - " + captureResult.Quality);
                    }
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Hookup capture handler and start capture.
        /// </summary>
        /// <param name="OnCaptured">Delegate to hookup as handler of the On_Captured event</param>
        /// <returns>Returns true if successful; false if unsuccessful</returns>
        public bool StartCaptureAsync(Reader.CaptureCallback OnCaptured)
        {
            using (Tracer tracer = new Tracer("Check::StartCaptureAsync"))
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
        /// Handler for when a fingerprint is captured.
        /// </summary>
        /// <param name="captureResult">contains info and data on the fingerprint capture</param>
        private void OnCaptured(CaptureResult captureResult)
        {
            try
            {
                if (!CheckCaptureResult(captureResult)) return;

                count++;

                DataResult<Fmd> resultConversion = FeatureExtraction.CreateFmdFromFid(captureResult.Data, Constants.Formats.Fmd.ANSI);

                SendMessage(Action.SendMessage, "La huella fue capturada.  \r\nCount:  " + (count));

                if (resultConversion.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    if (resultConversion.ResultCode != Constants.ResultCode.DP_TOO_SMALL_AREA)
                    {
                        Reset = true;
                    }
                    throw new Exception(resultConversion.ResultCode.ToString());
                }

                huella_capturada = resultConversion.Data;

                var empleadosAll = DatoEmpleado.MuestraEmpleados();

                foreach (var item in empleadosAll)
                {
                    if (item.xmlFmd != null && item.xmlFmd != "")
                    {
                        Fmd val = Fmd.DeserializeXml(item.xmlFmd);
                        _sender.Fmds.Add(item.Id, val);
                    }
                }

                // See the SDK documentation for an explanation on threshold scores.
                int thresholdScore = DPFJ_PROBABILITY_ONE * 1 / 100000;

                IdentifyResult identifyResult = Comparison.Identify(huella_capturada, 0, _sender.Fmds.Values, thresholdScore, 1);
                if (identifyResult.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    Reset = true;
                    throw new Exception(identifyResult.ResultCode.ToString());
                }
                var res = _sender.Fmds.Values.ToList();
                var res2 = res[identifyResult.Indexes[0][0]];
                Empleado empleado = empleadosAll.Where(x => x.Id == _sender.Fmds.FirstOrDefault(y => y.Value == res2).Key).FirstOrDefault();
                SendMessage(Action.SendMessage, "Se encontraron los siguientes usuarios: " + identifyResult.Indexes.Length.ToString());
                
                SendMessage(Action.SendMessage, "Hola " + empleado.Nombres + " " + empleado.Apellidos + " bienvenido. -- Hora: 00:00");
                
                // Muestra los datos del usuario.
                this.Dispatcher.Invoke(() =>
                {
                    Desplegar(empleado);
                });

                SendMessage(Action.SendMessage, "Espere 5 segundos, por favor...");
                // Espera 5 seg.
                System.Threading.Thread.Sleep(5000);
                
                // Muestra los datos por default
                this.Dispatcher.Invoke(() =>
                {
                    ResetUI();
                });
            }
            catch (Exception ex)
            {
                // Send error message, then close form
                SendMessage(Action.SendMessage, "Error:  " + ex.Message);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //txtIdentify.Text = string.Empty;
            //rightIndex = null;
            count = 0;
            LoadScanners();
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
                this.Close();
            }
            // Escucha por la captura de huella.
            if (!StartCaptureAsync(this.OnCaptured))
            {
                this.Close();
            }

            if (identificationControl != null)
            {
                
                identificationControl.Reader = CurrentReader;
            }
            else
            {
                SendMessage(Action.SendMessage, "Pon tu dedo registrado en el lector.");   
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {

        }
    }
}
