using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Checador.Models
{
    public class Empleado
    {
        public Empleado() { }

        public int Id { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set;}
        public string Numero {  get; set; }
        public string Foto { get; set;}
        public byte[] Huella { get; set;}
    }
}
