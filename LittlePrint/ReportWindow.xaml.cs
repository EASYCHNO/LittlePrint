using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LittlePrint
{
    /// <summary>
    /// Логика взаимодействия для ReportWindow.xaml
    /// </summary>
    public partial class ReportWindow : Window
    {
        public DateTime StartDate => startDatePicker.SelectedDate.Value;
        public DateTime EndDate => endDatePicker.SelectedDate.Value;
        public ReportWindow()
        {
            InitializeComponent();
        }
        private void GenerateReportButton_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, чтобы даты были выбраны
            if (startDatePicker.SelectedDate.HasValue && endDatePicker.SelectedDate.HasValue)
            {
                DialogResult = true; 
            }
        }
    }
}
