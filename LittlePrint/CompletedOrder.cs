using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LittlePrint
{
    public class CompletedOrder
    {
        public int CompletedOrderID { get; set; }
        public int? OrderID { get; set; } // Nullable для локальных заказов
        public int? LocalOrderID { get; set; } // Nullable для обычных заказов
        public string FullName { get; set; }
        public DateTime PrintDate { get; set; }
        public int PaperCount { get; set; }
        public double TotalPrice { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Lastname { get; set; }
        public string LocalFileName { get; set; }
        public bool OwnPaper { get; set; }
        public bool IsLocal { get; set; }
        public string IsLocalString
        {
            get
            {
                if (IsLocal)
                    return "Локальный";
                else return "Онлайн";
            }
        }
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
