using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LittlePrint
{
    public class OrderWithFile
    {
        public int OrderID { get; set; }
        public string Login { get; set; }
        public DateTime OrderDate { get; set; }
        public string StatusName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int FileID { get; set; }
        public int UserID { get; set; }
        public int StatusID { get; set; }
        public string Surname { get; set; }
        public string Lastname { get; set; }
        public string Name { get; set; }
        public string OrdersFullName
        {
            get
            {
                { return $"{Surname} {Name} {Lastname}"; }
            }
            set { }
        }
        public double OrderPrice { get; set; }
    }
}
