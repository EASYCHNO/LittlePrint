using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LittlePrint
{
    public class LocalOrders
    {
        public int LocalOrderID { get; set; }
        public string LocalFileName { get; set; }
        public DateTime LocalOrderDate { get; set; }
        public double LocalOrderPrice { get; set; } 
        public bool OwnPaper {  get; set; }
        public string OwnPaperString
        {
            get
            {
                if (OwnPaper)
                    return "Да";
                else return "Нет";
            }
        }
    }
}
