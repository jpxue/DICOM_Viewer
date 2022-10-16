using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace MAUI_DICOM_Viewer.Pages
{
    public class FileFolderView
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public bool IsFolder { get; set; }
        public ImageSource Icon { get; set; }
        public string Info { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsNotSelected { get; set; }
        public bool HasChildren { get; private set; }

        public FileFolderView(string location, bool isFolder, bool selected = false)
        {
            Location = location;
            IsFolder = isFolder;
            Name = Path.GetFileName(location); //works for both folders and files :)
            Icon = isFolder ? "ic_folder.png" : "ic_file.png";
            IsSelected = selected;
            IsNotSelected = !selected;
            HasChildren = false;

            try
            {
                if (isFolder)
                {
                    int subFilesCount = Directory.GetFiles(location).Length;
                    int subFolderCount = Directory.GetDirectories(location).Length;

                    HasChildren = subFolderCount + subFilesCount > 0;

                    string subDirInfo = string.Empty;
                    if (subFolderCount > 0)
                        subDirInfo = $"{subFolderCount} Folders";
                    if (subFilesCount > 0)
                        subDirInfo += $"{((Info.Length > 0) ? " | " : "")}{subFilesCount} Files";

                    Info = subDirInfo;
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(Location);
                    long bytes = fileInfo.Length;

                    Info = SizeSuffix(bytes, 2);

                }
            }
            catch (UnauthorizedAccessException) { }
        }

        //Credit: https://stackoverflow.com/questions/14488796/does-net-provide-an-easy-way-convert-bytes-to-kb-mb-gb-etc
        static readonly string[] SizeSuffixes =
                  { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
        static string SizeSuffix(long value, int decimalPlaces = 1)
        {
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }

        public override string ToString()
        {
            return $"{Name}: {Location}";
        }
    }
}
