using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp3
{
    public enum enum1 {spicy, salty, sweet, sour, bitter, creamy, coffee, citrus, neutral };
    [Serializable]
    public class Cocktails
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int degree { get; set; }
        public byte[] picture { get; set; }
        public int volume { get; set; }
        public string receipt { get; set; }
        public string group { get; set; }
        public int basis_id { get; set; }
        //public enum1 taste { get; set; }
        public string taste { get; set; }
    }
}
