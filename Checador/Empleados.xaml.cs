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

namespace Checador
{
    /// <summary>
    /// Lógica de interacción para Empleados.xaml
    /// </summary>
    public partial class Empleados : Window
    {
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
                MessageBox.Show("El campo Nombres es necesario.", "Error Nombres");
                return;
            }
            if (tbApellidos.Text == "")
            {
                MessageBox.Show("El campo Apellidos es necesario.", "Error Apellidos");
                return;
            }
            if (tbNumero.Text == "")
            {
                MessageBox.Show("El campo Número Empleado es necesario.", "Error Número Empleado");
                return;
            }

            try
            {
                Empleado empleado = new Empleado();
                empleado.Nombres = tbNombres.Text;
                empleado.Apellidos = tbApellidos.Text;
                empleado.Numero = tbNumero.Text;
                empleado.Foto = tbUrlFoto.Text;
                empleado.Huella = null;

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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el empleado: " + ex.Message, "Algo salio mal :(");
            }
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
                //enrollmentControl._sender = this;
            }

            enrollmentControl.ShowDialog();
            enrollmentControl.Dispose();
            enrollmentControl = null;
        }
    }
}
