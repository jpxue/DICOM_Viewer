using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

using FellowOakDicom;
using FellowOakDicom.Imaging;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;

namespace MAUI_DICOM_Viewer.Pages
{
    public class FrameCollection
    {
        public List<Frame> Frames = new List<Frame>();

        //Progress counters
        private int _total = 0;
        private int _current = 0;

        public FrameCollection()
        {
            Frames = new List<Frame>();
        }

        public async Task FromFiles(List<string> files)
        {
            _progessModelReset();
            _total = files.Count;
            _infoModelUpdated = false;

            //Assign 'Roots'
            UI.ClearRoots();
            HashSet<string> parentDirs = new HashSet<string>();
            foreach(string file in files)
            {
                string parentFolderPath = Directory.GetParent(file).ToString();
                parentDirs.Add(parentFolderPath);
            }

            foreach(string parentDir in parentDirs)
            {
                string lastFolderName = new DirectoryInfo(parentDir).Name;
                ImageRoot root = new ImageRoot(parentDir, "", lastFolderName);
                UI.ImageRoots.Add(root);
            }

            string prevDir = string.Empty;
            for(int i = 0; i < files.Count; i++)
            {
                string dir = Path.GetDirectoryName(files[i]);
                if(dir!=prevDir)
                {
                    _actualFrameNo = 0;
                    prevDir = dir;
                }

                await _fromFile(files[i], false);
            }

            //Populate ImageView from the valid 'Roots' (these could have been altered in _fromFile hence why its imp to populate after processing
            List<string> lastParentFolderNameOnly = new List<string>();
            foreach(var _root in UI.ImageRoots)
                UI.RootList.Add(_root.DisplayString);
        }

        public async Task FromFile(string fileLoc)
        {
            _progessModelReset();
            _infoModelUpdated = false;

            //Roots View Model
            UI.ClearRoots();
            ImageRoot root = new ImageRoot(Directory.GetParent(fileLoc).ToString(), fileLoc, Path.GetFileName(fileLoc));
            UI.ImageRoots.Add(root);

            _actualFrameNo = 0;
            await _fromFile(fileLoc, true);

            //Populate ViewModel based on ImageRoots
            foreach(var _root in UI.ImageRoots)
                UI.RootList.Add(_root.DisplayString);
        }

        private void _progessModelReset()
        {
            UI.ProgressView.PFloat = 0.0f;
            UI.ProgressView.PText = "";
            UI.ProgressView.PPercent = "0%";
        }


        /// <summary>
        /// Update progress model view (UI).
        /// </summary>
        /// <param name="val">Images processed</param>
        /// <param name="total">Total images to process</param>
        private void _progessModelUpdate(int val, int total)
        {
            float prog = ((float)val * 1.0f) / (float)total;

            int percent = (int)Math.Floor(prog * 100);
            UI.ProgressView.PPercent = $"{percent.ToString("0")}%"; ;
            UI.ProgressView.PText = $"Image: {val}/{total}";
            UI.ProgressView.PFloat = prog;
        }

        private void _infoModelUpdate(DicomTags tags)
        {
            UI.InfoView.PatientInfo = tags.PatientDetails;
            UI.InfoView.StudyInfo = tags.StudyDetails;
        }

        private bool _infoModelUpdated = false;
        private int _actualFrameNo = 0; //Folder mode 
        private async Task _fromFile(string fileLoc, bool onlyFile=true)
        {
            try
            {
                DicomImage imgs = new DicomImage(fileLoc);

                DicomFile file = DicomFile.Open(fileLoc);
                DicomDataset dataset = file.Dataset;
                DicomTags tags = new DicomTags(dataset);

                if (onlyFile)
                    _total = imgs.NumberOfFrames;


                if (!_infoModelUpdated)
                {
                    _infoModelUpdate(tags);
                    _infoModelUpdated = true;
                }

                string saveDir = Caching.GetCachingDirectory(fileLoc);
                string fileName = Path.GetFileName(fileLoc);

                Caching.IncompleteCacheCheck(saveDir, fileName, imgs.NumberOfFrames);

                for (int i = 0; i < imgs.NumberOfFrames; i++)
                {
                    _progessModelUpdate(_current + 1, _total); //avoids zero exception
                    if (onlyFile)
                        ++_current;

                    string frameLoc = $"{saveDir}{fileName}_frame{i}.jpg";
                    if (File.Exists(frameLoc)) //already in cache
                    {
                        long fileSz = new FileInfo(frameLoc).Length;
                        if (fileSz > 0) //simple integrity check
                        {
                            Frames.Add(new Frame(frameLoc, fileLoc, tags, 
                                File.ReadAllBytes(frameLoc), _actualFrameNo++, imgs.Width, imgs.Height));
                            continue;
                        }
                    }

                    if (_isTransferSyntaxSupported(dataset.InternalTransferSyntax))
                    {
                        //FO-DICOM transcoders
                        Image<Bgra32> sharpImg = imgs.RenderImage(i).AsSharpImage();
                        sharpImg.SaveAsJpeg(frameLoc);
                    }
                    else
                    {
                        //LibJPEG transcoders
                        DicomPixelData pixData = DicomPixelData.Create(file.Dataset, false);
                        var byteBuffer = pixData.GetFrame(i);

                        using (var outStream = new FileStream(frameLoc, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                        using (var jpgStream = new MemoryStream(byteBuffer.Data))
                        using (var jpegImg = new BitMiracle.LibJpeg.JpegImage(jpgStream))
                        {
                            jpegImg.WriteJpeg(outStream);
                        }
                    }

                    Frames.Add(new Frame(frameLoc, fileLoc, tags, File.ReadAllBytes(frameLoc), 
                        _actualFrameNo++, imgs.Width, imgs.Height));
                }
            }
            catch (FellowOakDicom.Imaging.DicomImagingException)
            {
                if (onlyFile)
                {
                    //Remove from roots
                    int idxToRemove = UI.ImageRoots.FindIndex(root => root.FilePath == fileLoc);
                    if (idxToRemove != -1)
                        UI.ImageRoots.RemoveAt(idxToRemove);
                }
            }

            if (!onlyFile)
                ++_current;

        }

        private static string[] _supportedUIDs = new string[] { 
            "1.2.840.10008.1.2", 
            "1.2.840.10008.1.2.1", 
            "1.2.840.10008.1.2.1.99", 
            "1.2.840.10008.1.2.2", 
            "1.2.840.10008.1.2.5" 
        };
        private bool _isTransferSyntaxSupported(DicomTransferSyntax syntax)
        {
            //all codecs supported on Win, MAC, Linux, otherwise for mobile we have to use LibJPEG for unsupported formats
            return (DeviceInfo.Current.Idiom == DeviceIdiom.Desktop) ? true : _supportedUIDs.Contains(syntax.UID.UID);
        }

    }

}
