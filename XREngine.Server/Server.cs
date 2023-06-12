using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XREngine.Server
{
    public class Server
    {
        public string? IP { get; set; }
        public int Port { get; set; }
        public int CurrentLoad { get; set; } = 0;
        public List<Room> Rooms { get; set; } = new List<Room>();
    }
}
