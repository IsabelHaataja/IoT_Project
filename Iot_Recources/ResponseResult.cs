using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot_Recources
{
    public class ResponseResult<T>
    {
        public bool Succeeded { get; set; }
        public T? Result { get; set; }
        public string? Error { get; set; }
    }
}
