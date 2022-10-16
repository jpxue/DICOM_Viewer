using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Security.Cryptography;

namespace MAUI_DICOM_Viewer.Pages
{
    internal class Caching
    {
        public static string GetCachingDirectory(string fileLoc)
        {
            string uniqueID = _getUniqueDirID(fileLoc);
            string saveDir = string.Empty;

#if ANDROID || IOS
            saveDir = @$"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}/dicom_cache/{uniqueID}";
#else   
            saveDir = @$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/dicom_cache/{uniqueID}";
#endif

            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            return saveDir.EndsWith("/") ? saveDir : string.Concat(saveDir, "/");
        }

        private static string _getUniqueDirID(string path)
        {
            FileInfo fileInfo = new FileInfo(path);
            long size = fileInfo.Length;

            string dir = Path.GetDirectoryName(path); //We can rely on dir being constant and thus factoring it in our algorithm unlike if we used FilePicker :)

            string uniquePath = SHA1String(dir); //Hash the directory string so it is shorter
            return uniquePath.EndsWith("/") ? uniquePath : string.Concat(uniquePath, "/");
        }

        private static string SHA1String(string str)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hash = sha1.ComputeHash(Encoding.Default.GetBytes(str));
                return Convert.ToHexString(hash);
            }
        }

        /// <summary>
        /// If we have an incompletely cached set of frames lets just delete the last one for good measure.
        /// This frame is usually incomplete as a result of an unexpected stop whilst writing the filestream.
        /// </summary>
        public static void IncompleteCacheCheck(string saveDir, string fileName, int frameCount)
        {
            //If starting frame is present but not the end frame => (incomplete cache)
            if (File.Exists($"{saveDir}{fileName}_frame0.jpg")
                && !File.Exists($"{saveDir}{fileName}_frame{frameCount - 1}.jpg"))
            {
                for (int i = frameCount - 2; i >= 0; i--)
                {
                    string frameLoc = $"{saveDir}{fileName}_frame{i}.jpg";
                    if (File.Exists(frameLoc))
                    {
                        File.Delete(frameLoc);
                        return;
                    }
                }

            }
        }
    }
}
