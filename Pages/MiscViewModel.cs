using DDR4XMPEditor.DDR4SPD;
using Stylet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace DDR4XMPEditor.Pages
{
    public class MiscViewModel : Screen
    {
        public bool IsEnabled { get; set; }

        public SPD SPD { get; set; }

        public ObservableCollection<Tuple<string, SPD.Densities>> DensityCollection { get; set; }
        public ObservableCollection<int> BanksCollection { get; set; }
        public ObservableCollection<int> BankGroupsCollection { get; set; }
        public ObservableCollection<int> ColumnAddressesCollection { get; set; }
        public ObservableCollection<int> RowAddressesCollection { get; set; }

        public SPD.Densities SelectedDensity 
        { 
            get => SPD != null && SPD.Density.HasValue ? SPD.Density.Value : SPD.Densities._256Mb;
            set => SPD.Density = value;
        }
        public int SelectedBank 
        { 
            get => SPD != null ? SPD.Banks : 2;
            set => SPD.Banks = value; 
        }
        public int SelectedBankGroups 
        { 
            get => SPD != null ? SPD.BankGroups : 0; 
            set => SPD.BankGroups = value; 
        }
        public int SelectedColumnAddress 
        { 
            get => SPD != null ? SPD.ColumnAddresses : 9; 
            set => SPD.ColumnAddresses = value; 
        }
        public int SelectedRowAddress
        { 
            get => SPD != null ? SPD.RowAddresses : 12; 
            set => SPD.RowAddresses = value; 
        }

        public MiscViewModel()
        {
            DensityCollection = new ObservableCollection<Tuple<string, SPD.Densities>>
            {
                Tuple.Create("256Mb", SPD.Densities._256Mb),
                Tuple.Create("512Mb", SPD.Densities._512Mb),
                Tuple.Create("1Gb", SPD.Densities._1Gb),
                Tuple.Create("2Gb", SPD.Densities._2Gb),
                Tuple.Create("4Gb", SPD.Densities._4Gb),
                Tuple.Create("8Gb", SPD.Densities._8Gb),
                Tuple.Create("16Gb", SPD.Densities._16Gb),
                Tuple.Create("32Gb", SPD.Densities._32Gb),
                Tuple.Create("12Gb", SPD.Densities._12Gb),
                Tuple.Create("24Gb", SPD.Densities._24Gb)
            };

            BanksCollection = new ObservableCollection<int>(new int[2] { 4, 8 });
            BankGroupsCollection = new ObservableCollection<int>(new int[3] { 0, 2, 4 });

            ColumnAddressesCollection = new ObservableCollection<int>(Enumerable.Range(9, 4));
            RowAddressesCollection = new ObservableCollection<int>(Enumerable.Range(12, 7));
        }
    }
}
