using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimuladorRedes.Clases
{
    [Flags]
    public enum FTPPermiso
    {
        Ninguno = 0,
        Ver = 1,
        Editar = 2,
        Eliminar = 4,
        Todos = Ver | Editar | Eliminar
    }
}
