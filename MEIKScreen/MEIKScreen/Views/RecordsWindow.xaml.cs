﻿using MEIKScreen.Common;
using MEIKScreen.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MEIKScreen
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class RecordsWindow : Window
    {        
        private ObservableCollection<Patient> patientCollection;
        private Dictionary<string, int> countDict = new Dictionary<string, int>();
        private IList<Patient> patientList=new List<Patient>();
        //private int times = 0;

        #region public Properties
        public ObservableCollection<Patient> PatientCollection
        {
            get
            {
                return this.patientCollection;
            }
            set
            {
                this.patientCollection = value;                
            }
        }
       
        #endregion

        public RecordsWindow()
        {            
            InitializeComponent();
            if (App.strDiagnostics != "Diagnostics")
            {
                fromDate.FormatString = "yyyy年MM月dd日";
                toDate.FormatString = "yyyy年MM月dd日";
            }


            CountScreeningTimes(new DateTime(), DateTime.Now);                        
        }

        private void CountScreeningTimes(DateTime fromDate, DateTime toDate)
        {                        
            try
            {
                patientList.Clear();
                ListFiles(new DirectoryInfo(App.reportSettingModel.DataBaseFolder), fromDate, toDate); 
                                
                patientCollection = new ObservableCollection<Patient>(patientList);
                this.patientGrid.ItemsSource = patientCollection;
                this.txtTimes.Text = patientList.Count + "";
                this.patientTimes.Text = countDict.Count + "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to count the screening times. Exception: " + ex.Message);
            }
        }

        private void ListFiles(FileSystemInfo info, DateTime fromDate, DateTime toDate)
        {
            if (!info.Exists) return;
            DirectoryInfo dir = info as DirectoryInfo;
            //不是目录 
            if (dir == null) return;
            if (!dir.Name.Equals("NORM"))
            {
                FileSystemInfo[] files = dir.GetFileSystemInfos();
                for (int i = 0; i < files.Length; i++)
                {
                    FileInfo file = files[i] as FileInfo;
                    //是文件 
                    if (file != null)
                    {
                        if (".tdb".Equals(file.Extension, StringComparison.OrdinalIgnoreCase))
                        {
                            if (file.LastWriteTime >= fromDate && file.LastWriteTime <= toDate)
                            {
                                FileStream fsRead = new FileStream(file.FullName, FileMode.Open);
                                byte[] nameBytes = new byte[105];
                                byte[] codeBytes = new byte[11];
                                byte[] descBytes = new byte[200];
                                fsRead.Seek(12, SeekOrigin.Begin);
                                fsRead.Read(nameBytes, 0, nameBytes.Count());
                                fsRead.Seek(117, SeekOrigin.Begin);
                                fsRead.Read(codeBytes, 0, codeBytes.Count());
                                fsRead.Seek(129, SeekOrigin.Begin);
                                fsRead.Read(descBytes, 0, descBytes.Count());
                                fsRead.Close();
                                string name = System.Text.Encoding.ASCII.GetString(nameBytes);
                                name = name.Split("\0".ToCharArray())[0];
                                string code = System.Text.Encoding.ASCII.GetString(codeBytes);
                                string desc = System.Text.Encoding.ASCII.GetString(descBytes);
                                desc = desc.Split("\0".ToCharArray())[0];
                                var patient = new Patient();
                                patient.Code = code;
                                patient.Name = name;
                                patient.Desc = desc;
                                patient.ScreenDate = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                                patientList.Add(patient);
                                var key = code + ";" + name;
                                if (countDict.ContainsKey(key))
                                {
                                    countDict[key] = countDict[key] + 1;
                                }
                                else
                                {
                                    countDict.Add(key, 1);
                                }
                            }
                            //times++;
                        }
                    }
                    //对于子目录，进行递归调用 
                    else
                    {
                        ListFiles(files[i], fromDate, toDate);
                    }
                }
            }
        }

        private void export_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog() { Filter = "xls|*.xls" };
            if (dlg.ShowDialog(this) == true)
            {                
                var excelApp = new Microsoft.Office.Interop.Excel.Application();
                var books = (Microsoft.Office.Interop.Excel.Workbooks)excelApp.Workbooks;
                var book = (Microsoft.Office.Interop.Excel._Workbook)(books.Add(System.Type.Missing));
                var sheets = (Microsoft.Office.Interop.Excel.Sheets)book.Worksheets;
                var sheet = (Microsoft.Office.Interop.Excel._Worksheet)(sheets.get_Item(1));


                Microsoft.Office.Interop.Excel.Range range = (Microsoft.Office.Interop.Excel.Range)sheet.get_Range("A1", "A100");
                range.ColumnWidth = 15;
                range = (Microsoft.Office.Interop.Excel.Range)sheet.get_Range("B1", "B100");
                range.ColumnWidth = 30;
                range = (Microsoft.Office.Interop.Excel.Range)sheet.get_Range("C1", "C100");
                range.ColumnWidth = 20;
                range = (Microsoft.Office.Interop.Excel.Range)sheet.get_Range("D1", "D100");
                range.ColumnWidth = 100;
                sheet.Cells[1, 1] = App.Current.FindResource("Excel1").ToString();
                sheet.Cells[1, 2] = App.Current.FindResource("Excel2").ToString();
                sheet.Cells[1, 3] = App.Current.FindResource("Excel3").ToString();
                sheet.Cells[1, 4] = App.Current.FindResource("Excel4").ToString();
                for (int i = 0; i < patientList.Count; i++)
                {
                    Patient item = patientList[i];
                    sheet.Cells[i + 2, 1] = item.Code;
                    sheet.Cells[i + 2, 2] = item.Name;
                    sheet.Cells[i + 2, 3] = item.ScreenDate;
                    sheet.Cells[i + 2, 4] = item.Desc;
                }
                book.SaveAs(dlg.FileName, Microsoft.Office.Interop.Excel.XlFileFormat.xlExcel8, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange, Type.Missing, Type.Missing, Type.Missing, Type.Missing, Type.Missing);
                book.Close();
                excelApp.Quit();
            }            

        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            countDict.Clear();
            patientList.Clear();
            DateTime startDate = string.IsNullOrEmpty(fromDate.Text) ? new DateTime() : DateTime.Parse(fromDate.Text);
            DateTime endDate = string.IsNullOrEmpty(toDate.Text) ? new DateTime() : DateTime.Parse(toDate.Text);
            CountScreeningTimes(startDate, endDate);
        } 


    }
}
