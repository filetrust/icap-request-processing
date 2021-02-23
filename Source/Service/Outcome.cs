using System;
using System.Collections.Generic;
using System.Text;

namespace Service
{
    public class Outcome
    {
        public string Status { get; set; }
        public Dictionary<string,string> OptionalHeaders { get; set; }

        public Outcome()
        {
            OptionalHeaders = new Dictionary<string, string>();
        }
    }
}
