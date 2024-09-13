﻿using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Knossos.NET.Models;
using Knossos.NET.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Knossos.NET.ViewModels
{
    public partial class DevModAdvancedUploadData : ObservableObject
    {
        [ObservableProperty]
        internal List<ComboBoxItem> otherVersions = new List<ComboBoxItem>();

        internal int otherVersionsSelectedIndex = 0;
        internal int OtherVersionsSelectedIndex
        {
            get { return otherVersionsSelectedIndex; }
            set
            {
                if (otherVersionsSelectedIndex != value)
                {
                    this.SetProperty(ref otherVersionsSelectedIndex, value);
                    if (value <= 1 ) //Manual Input // Auto
                    {
                        CustomHash = "";
                    }
                    else
                    {
                        try
                        {
                            //Mod class is saved in the datacontext for each version
                            if (OtherVersions[value].DataContext != null)
                            {
                                var modPackage = (ModPackage)OtherVersions[value].DataContext!;
                                if (modPackage != null && modPackage.files != null)
                                {
                                    CustomHash = modPackage.files.FirstOrDefault()!.checksum![1].ToString();
                                }
                                else
                                {
                                    CustomHash = "Error getting hash";
                                }
                            }
                            else
                            {
                                CustomHash = "Error getting hash";
                            }
                        }
                        catch (Exception ex)
                        {
                            CustomHash = "Error getting hash";
                            Log.Add(Log.LogSeverity.Error, "DevModAdvancedUploadData(OtherVersionsSelectedIndex.Setter)", ex);
                        }
                    }
                }
            }
        }

        internal bool upload = true;
        internal bool Upload
        {
            get { return upload; }
            set
            {
                if (upload != value)
                {
                    this.SetProperty(ref upload, value);
                    try
                    {
                        if (value == false)
                        {
                            //Enable all Versions listed in the combobox
                            OtherVersions.ForEach(o => o.IsEnabled = true);
                            OtherVersions[0].IsEnabled = false; // Disable "auto"
                            CustomHash = "";
                            //Select what should be the latest version of a mod id
                            OtherVersionsSelectedIndex = OtherVersions.Count() - 1;
                        }
                        else
                        {
                            //Disable all versions listed in the combobox
                            OtherVersions.ForEach(o => o.IsEnabled = false);
                            OtherVersions[0].IsEnabled = true; //Enable Auto
                            //Select Auto
                            OtherVersionsSelectedIndex = 0;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Add(Log.LogSeverity.Error, "DevModAdvancedUploadData(Upload.Setter)", ex);
                    }
                }
            }
        }
        
        [ObservableProperty]
        internal string customHash = "";

        public ModPackage? package;
        public string PackageName
        {
            get
            {
                if (package != null)
                    return package.name;
                else
                    return "package is null";
            }
        }


        public DevModAdvancedUploadData(ModPackage package, Mod mod)
        {
            this.package = package;

            //Fill Get Hash Combobox
            var auto = new ComboBoxItem();
            auto.Content = "Auto";
            auto.IsEnabled = true;
            OtherVersions.Add(auto);

            var manual = new ComboBoxItem();
            manual.Content = "Manual Input";
            manual.IsEnabled = false;
            OtherVersions.Add(manual);

            //Get all local versions of this ID that are uploaded to nebula, that devmode is enabled and contains this package name
            var versions = Knossos.GetInstalledModList(mod.id).Where( m => m.devMode && m.inNebula && m.packages != null && m.packages.FirstOrDefault(p => p.name == package.name) != null);

            if (versions != null)
            {
                //Reload json data to get the hashes
                versions.ForEach(m => m.ReLoadJson());
                foreach (var o in versions)
                {
                    if (o != mod)
                    {
                        try
                        {
                            //List version in the combobox
                            var p = o.packages.FirstOrDefault(p => p.name == package.name);
                            if (p != null && p.files != null && p.files.Any() && p.files.FirstOrDefault()?.checksum?.Length > 0)
                            {
                                var item = new ComboBoxItem();
                                item.DataContext = p;
                                item.Content = o.version.ToString();
                                item.IsEnabled = false;
                                item.Foreground = o.isPrivate ? Brushes.Red : Brushes.Cyan;
                                OtherVersions.Add(item);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Add(Log.LogSeverity.Error, "DevModAdvancedUploadData(contructor)", ex);
                        }
                    }
                }
            }
            OtherVersionsSelectedIndex = 0;
        }
    }

    /********************************************************************************/

    public partial class DevModAdvancedUploadViewModel : ViewModelBase
    {
        [ObservableProperty]
        internal string title = string.Empty;
        [ObservableProperty]
        internal bool loading = true;
        [ObservableProperty]
        internal int parallelCompressing = 1;
        [ObservableProperty]
        internal int parallelUploads = 1;
        [ObservableProperty]
        internal List<DevModAdvancedUploadData> modPackagesData  = new List<DevModAdvancedUploadData>();

        private Mod? uploadMod = null;
        private DevModAdvancedUploadView? dialog = null;

        public DevModAdvancedUploadViewModel() 
        {
        }

        public DevModAdvancedUploadViewModel(Mod mod, DevModAdvancedUploadView dialog)
        {
            this.dialog = dialog;
            Title = "Advanced Nebula Upload: " + mod;
            uploadMod = mod;
            _ = Task.Run(() => { LazyLoading(); });
        }

        private void LazyLoading()
        {
            if (uploadMod != null && uploadMod.packages != null)
            {
                foreach (var package in uploadMod.packages)
                {
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        var data = new DevModAdvancedUploadData(package, uploadMod);
                        ModPackagesData.Add(data);
                    });
                }
            }
            Loading = false;
        }

        internal async void UploadMod()
        {
            //We have to check that all packages we arent going to upload has a valid sha256 and that they had been uploaded to nebula
            //Basic check
            foreach (var data in ModPackagesData)
            {
                if (!data.upload)
                {
                    if (String.IsNullOrWhiteSpace(data.CustomHash) || data.CustomHash.Length == 0)
                    {
                        _ = MessageBox.Show(MainWindow.instance!, "Package: " + data.PackageName + " is not set to upload but it lacks a defined sha256. Operation cancelled.", "Missing hash data", MessageBox.MessageBoxButtons.OK);
                        return;
                    }
                    else
                    {
                        //ensure it is in lowercase
                        data.CustomHash = data.CustomHash.ToLower();
                    }
                }
            }

            //Check with nebula
            foreach (var data in ModPackagesData)
            {
                if (!data.upload)
                {
                    if(await Nebula.IsFileUploaded(data.CustomHash) == false)
                    {
                        _ = MessageBox.Show(MainWindow.instance!, "The provided sha256 hash for package: " + data.PackageName + " is not valid or not uploaded to Nebula, operation cancelled. Passed hash:" + data.CustomHash, "File hash is not uploaded to nebula (or it is incorrect)", MessageBox.MessageBoxButtons.OK);
                        Log.Add(Log.LogSeverity.Error,"DevModAdvancedUploadViewModel.UploadMod", "The provided sha256 hash for package: " + data.PackageName + " is not valid or not uploaded to Nebula, operation cancelled. Passed hash:"+data.CustomHash);
                    }
                    await Task.Delay(1000);
                }
            }
        }
    }
}
