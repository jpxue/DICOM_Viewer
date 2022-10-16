using MAUI_DICOM_Viewer.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_DICOM_Viewer
{
    public class Frame
    {
        public readonly string DicomLoc; //Original file path
        public readonly string SavedFrameLoc; //Cached path
        public readonly string OriginalDirectory; //Original file directory

        public readonly byte[] Buffer;
        public readonly int Number;
        public readonly int Width, Height;

        //public readonly string Directory;

        public readonly DicomTags Info;

        public Frame(string fileLoc, string originaLoc, DicomTags info, byte[] buffer, int number, int width, int height)
        {
            SavedFrameLoc = fileLoc;
            DicomLoc = originaLoc;
            //Directory = Path.GetDirectoryName(fileLoc);
            OriginalDirectory = Path.GetDirectoryName(originaLoc);
            Info = info;
            Buffer = buffer;
            Number = number;
            Width = width;
            Height = height;

        }
    }
}
