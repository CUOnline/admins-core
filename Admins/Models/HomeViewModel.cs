using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Admins.Models
{
    public class HomeViewModel
    {
        public bool Authorized { get; set; }
        public string BaseCanvasUrl { get; set; }
        public string ApiToken { get; set; }
    }
}
