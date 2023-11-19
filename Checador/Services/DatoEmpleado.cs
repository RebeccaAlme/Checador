using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

using Checador.Models;
using System.Data.Common;
using System.Windows;

namespace Checador.Services
{
    public class DatoEmpleado
    {
        public DatoEmpleado() { }

        public static List<Empleado> MuestraEmpleados()
        {
            List<Empleado> listaEmpleados = new List<Empleado>();

            try
            {
                using (var conn = new SqlConnection("Data Source = localhost; initial catalog = Checador; Integrated Security = True"))
                {
                    conn.Open();
                    using(var cmd = conn.CreateCommand()) { 
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "MuestraEmpleados";

                        using(DbDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.HasRows) { 
                                while (dr.Read())
                                {
                                    Empleado empleado = new Empleado();
                                    empleado.Id = int.Parse(dr["Id"].ToString());
                                    empleado.Nombres = dr["Nombres"].ToString();
                                    empleado.Apellidos = dr["Apellidos"].ToString();
                                    empleado.Numero = dr["Numero"].ToString();
                                    empleado.Foto = dr["Foto"].ToString();
                                    if (dr["Huella"].ToString() != "")
                                        empleado.Huella = (byte[])dr["Huella"];
                                    else
                                        empleado.Huella = null;
                                    listaEmpleados.Add(empleado);
                                }
                            }
                        }
                    }
                }
            } catch (Exception ex)
            {
                MessageBox.Show("Error al consultar Empleados: " + ex.Message,"Error");
            }
            return listaEmpleados;
        }
        
        public static int AltaEmpleado(Empleado empleado)
        {
            int res = 0;

            try
            {
                using (var conn = new SqlConnection("Data Source = localhost; initial catalog = Checador; Integrated Security = True"))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "AltaEmpleados";
                        cmd.Parameters.AddWithValue("@Nombres", empleado.Nombres);
                        cmd.Parameters.AddWithValue("@Apellidos", empleado.Apellidos);
                        cmd.Parameters.AddWithValue("@Numero", empleado.Numero);
                        cmd.Parameters.AddWithValue("@Foto", empleado.Foto);
                        cmd.Parameters.AddWithValue("@Huella", empleado.Huella);

                        SqlParameter param = new SqlParameter("Id", SqlDbType.Int);
                        param.Value = 0;
                        param.Direction = ParameterDirection.Output;
                        cmd.Parameters.Add(param);

                        res = cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al dar de alta empleado: " + ex.Message, "Error en alta");
            }

            return res;
        }
    }
}
