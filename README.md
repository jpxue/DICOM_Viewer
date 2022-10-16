# MAUI DICOM Viewer
A mobile DICOM file image viewer built using the .NET Multi-platform App UI (MAUI) framework released 6 months ago. 

Being developed on Android OS 10.0 but still requires ample testing as it is challenging to find imaging files online without going through imaging banks.

Loads a set of DICOM files using the [fo-dicom](https://github.com/fo-dicom/fo-dicom) library, decodes the images using the in-built codecs (by [Efferent-Health](https://github.com/Efferent-Health/fo-dicom.Codecs) or in the case of mobile, [LibJPEG.NET](https://github.com/BitMiracle/libjpeg.net)) and caches the image data to physical storage. 
Image data is then displayed on a GraphicsView in which the frame can be changed via a slider or by swiping across the image itself. 

## Demonstration

[Video](https://www.youtube.com/watch?v=wFbUG_v2fn0) demonstrating loading of single files as well as a directory containing a CT scan.

<p align="center">
   <img src="https://github.com/jpxue/DICOM_Viewer/blob/main/demo.gif" alt="animated" width="400"/>
</p>

## To Do
- Extensive testing 
- Threading/Parallelism + numerous performance optimizations
- Ability to clear the cache and save disk space
- Implementation of window/level presets & ability to adjust these via gestures
- Zooming and panning
- ZIP/RAR support + other file formats 

## Credits
- [Fellow Oak DICOM](https://github.com/fo-dicom/fo-dicom) 
- [Bit Miracle LibJPEG.NET](https://github.com/BitMiracle/libjpeg.net)
