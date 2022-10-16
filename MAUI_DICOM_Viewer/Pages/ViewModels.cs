using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_DICOM_Viewer.Pages
{
    public static class UI
    {
        public static ProgressModel ProgressView = new ProgressModel();
        public static DicomInfoModel InfoView = new DicomInfoModel();
        public static List<string> RootList = new List<string>();

        public static List<ImageRoot> ImageRoots = new List<ImageRoot>();

        public static void ClearRoots()
        {
            ImageRoots.Clear();
            RootList.Clear();
        }
    }

    public class DicomInfoModel : INotifyPropertyChanged
    {
        public DicomInfoModel()
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private string patientInfo;
        public string PatientInfo
        {
            get { return patientInfo; }
            set
            {
                patientInfo = value;
                OnPropertyChanged("PatientInfo");
            }
        }

        private string studyInfo;
        public string StudyInfo
        {
            get { return studyInfo; }
            set
            {
                studyInfo = value;
                OnPropertyChanged("StudyInfo");
            }
        }

        public void Clear()
        {
            PatientInfo = string.Empty;
            StudyInfo = string.Empty;
        }


        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProgressModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ProgressModel()
        {
            PFloat = 0.0f;
            PText = "[Image 0/0]";
            PPercent = "0%";
        }

        private string ptext;
        public string PText
        {
            get { return ptext; }
            set
            {
                ptext = value;
                OnPropertyChanged("PText");
            }
        }

        private string ppercent;
        public string PPercent
        {
            get { return ppercent; }
            set
            {
                ppercent = value;
                OnPropertyChanged("PPercent");
            }
        }

        private float pfloat;
        public float PFloat
        {
            get { return pfloat; }
            set
            {
                pfloat = value;
                OnPropertyChanged("PFloat");
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ImageRoot
    {
        public readonly string RootFolder; // Folder
        public readonly string FullFolderPath; // /storage/0/emulated/Folder
        public readonly string FilePath; // /storage/0/emulated/Folder/dicom.dcm
        public readonly string DisplayString; // what shows in picker
        public bool IsFolder => FilePath == string.Empty;

        public ImageRoot(string fullFolderPath, string filePath, string displayString)
        {
            FullFolderPath = fullFolderPath;
            FilePath = filePath;
            DisplayString = displayString;

            if(IsFolder)
                RootFolder = new DirectoryInfo(fullFolderPath).Name;
        }
    }


}
