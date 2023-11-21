using Checador.Models;
using Microsoft.Win32;
using System;
using System.IO;
using System.Collections.Generic;
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
using DPUruNet;
using System.Text.RegularExpressions;

namespace Checador
{
    /// <summary>
    /// Lógica de interacción para Empleados.xaml
    /// </summary>
    public partial class Empleados : Window
    {
        public MainWindow _sender;
        public Empleados()
        {
            InitializeComponent();
        }

        private void btnFoto_Click(object sender, RoutedEventArgs e)
        {
            if (tbNumero.Text == "")
            {
                MessageBox.Show("El número de empleado es necesario.", "Error Número de empleado");
                return;
            }

            OpenFileDialog openFileDialog  = new OpenFileDialog();
            openFileDialog.Filter = "Archivos de imagen (.jpg)|*.jpg|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                tbUrlFoto.Text = "";
                try
                {
                    BitmapImage foto = new BitmapImage();
                    foto.BeginInit();
                    foto.UriSource = new Uri(openFileDialog.FileName);
                    foto.EndInit();
                    foto.Freeze();

                    imgFoto.Source = foto;
                    tbUrlFoto.Text = "foto_" + tbNumero.Text + ".jpg";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar la imagen: " + ex.Message, "Algo salio mal :(");
                }
            }
                
        }

        private void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (tbNombres.Text == "") {
                MessageBox.Show("Es necesario el campo Nombres.", "Falta Nombres");
                return;
            }
            if (tbApellidos.Text == "")
            {
                MessageBox.Show("Es necesario llenar el campo Apellidos.", "Falta Apellidos");
                return;
            }
            if (tbNumero.Text == "")
            {
                MessageBox.Show("Es necesario el Número de Empleado.", "Falta Número Empleado");
                return;
            }
            if (_sender == null || !_sender.Fmds.Any())
            {
                MessageBox.Show("Es necesario capturar una huella.", "Falta Captura Huella");
                return;
            }

            try
            {
                Empleado empleado = new Empleado();
                empleado.Nombres = tbNombres.Text;
                empleado.Apellidos = tbApellidos.Text;
                empleado.Numero = tbNumero.Text;
                empleado.Foto = tbUrlFoto.Text;
                empleado.Huella = _sender.Fmds[1].Bytes;
                empleado.xmlFmd = Fmd.SerializeXml(_sender.Fmds[1]);

                string destino = @"C:\Users\petit\Downloads\eorc-admin docentes\checador_fotos\" + tbUrlFoto.Text;
                File.Copy(imgFoto.Source.ToString().Replace("file:///",""), destino, true);
                
                int id = DatoEmpleado.AltaEmpleado(empleado);
                if (id > 0) {
                    MessageBox.Show("Empleado guardado correctamente.", "Todo bien :)");

                    tbNombres.Text = "";
                    tbApellidos.Text = "";
                    tbNumero.Text = "";
                    tbUrlFoto.Text = "";
                    imgFoto.Source = null;
                    imgVerHuella.Visibility = Visibility.Hidden;
                    dgEmpleados.DataContext = DatoEmpleado.MuestraEmpleados();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el empleado: " + ex.Message, "Algo salio mal :(");
            }
        }

        /// <summary>
        /// Close window.
        /// </summary>
        private void btnBack_Click(System.Object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgEmpleados.DataContext = DatoEmpleado.MuestraEmpleados();
        }


        private void dgEmpleados_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Empleado empleado = (Empleado)dgEmpleados.SelectedItem;

            try
            {
                if (empleado.Huella != null)
                    imgVerHuella.Visibility = Visibility.Visible;
                else
                    imgVerHuella.Visibility = Visibility.Hidden;
                if (empleado != null)
                {
                    tbNombres.Text = empleado.Nombres;
                    tbApellidos.Text = empleado.Apellidos;
                    tbNumero.Text = empleado.Numero;
                    tbUrlFoto.Text = empleado.Foto;
                }

                if (empleado.Foto != "" && empleado.Foto != null)
                {
                    BitmapImage foto = new BitmapImage();
                    foto.BeginInit();
                    foto.UriSource = new Uri(@"C:\Users\petit\Downloads\eorc-admin docentes\checador_fotos\" + empleado.Foto);
                    foto.EndInit();
                    imgFoto.Source = foto;
                }
                else
                {
                    imgFoto.Source = null;
                    tbUrlFoto.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar los datos: " + ex.Message, "Algo salio mal :(");
            }
        }

        
        EnrollmentControl enrollmentControl; // Para saber cuando alguna huella ya se registro.
        private void btnCaptura_Click(object sender, RoutedEventArgs e)
        {
            if (enrollmentControl == null)
            {
                enrollmentControl = new EnrollmentControl();
                enrollmentControl._sender = new MainWindow();
            }

            enrollmentControl.ShowDialog();
            enrollmentControl.Dispose();
            this._sender = enrollmentControl._sender;
            imgVerHuella.Visibility = Visibility.Visible;
            enrollmentControl = null;
        }
    }
}
