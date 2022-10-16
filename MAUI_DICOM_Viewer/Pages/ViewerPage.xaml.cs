namespace MAUI_DICOM_Viewer.Pages;

using System.IO;
using Microsoft.Maui.Controls;

using FellowOakDicom;
using FellowOakDicom.Imaging;

using System.Linq;


[QueryProperty(nameof(Location), "Location")]
public partial class ViewerPage : ContentPage
{
    public ViewerPage()
    {
        InitializeComponent();

        ProgBar.BindingContext = UI.ProgressView;
        GridProgBarLbls.BindingContext = UI.ProgressView;
        GridDicomInfo.BindingContext = UI.InfoView;
    }

    public static FrameCollection Images = new FrameCollection();

    //Passed on as URI param, N.B. in later versions of android this is another cached copy of the original -_- saved in /storage/emulated/0/data/com.app/cache/ or /data/data/com.app/cache
    string _location;
    public string Location { get => _location; set => _location = Uri.UnescapeDataString(value); }
    private bool IsFolder { get => Location.EndsWith("/"); }


    private static int _totalFrames = -1;
    private static int _currentFrame = -1;
    //private string _loadedLocation = string.Empty;

    //Statics
    public static float DesiredWidth = 0.0f;
    public static bool CanDraw = false;

    private bool _servicesRegistered = false;
    private double _startX, _startY;

    [Obsolete]
    protected override async void OnAppearing()
    {
        /* Adding Transcoder Managers to run on every NetCorePlatform (https://github.com/fo-dicom/fo-dicom/wiki/Supported-Transfer-Syntaxes)
         * Also adding ImageSharp support because of a lack of standard classes that I am used to. */
        if (!_servicesRegistered)
        {
            new DicomSetupBuilder()
                .RegisterServices(s => s.AddFellowOakDicom()
                    .AddTranscoderManager<FellowOakDicom.Imaging.NativeCodec.NativeTranscoderManager>()
                    .AddImageManager<ImageSharpImageManager>())
                .SkipValidation()
                .Build();
            _servicesRegistered = true;
        }

        //if (Location == _loadedLocation) //if viewer is already loaded with the same file
        //    return;

        await Task.Run(() => LoadLocation(Location));
    }

    private void _loadFromRoot()
    {
        if (Images.Frames.Count == 0)
            return;

        int rootIdx = PickerRoot.SelectedIndex;
        if (rootIdx >= 0)
        {
            ImageRoot root = UI.ImageRoots[rootIdx];

            if (root.IsFolder)
            {
                _totalFrames = Images.Frames.Count(frame => frame.OriginalDirectory == root.FullFolderPath);
                _currentFrame = 0;
            }
            else
            {
                _totalFrames = Images.Frames.Count(frame => frame.DicomLoc == root.FilePath);
                _currentFrame = 0;
            }
        }

        GridSlider.IsVisible = _totalFrames > 1;
        if (GridSlider.IsVisible)
        {
            SliderFrame.Minimum = 1.0d;
            SliderFrame.Value = 0.0d;
            SliderFrame.Maximum = (double)_totalFrames;
            LblEndSlider.Text = SliderFrame.Maximum.ToString("#");
            LblStartSlider.Text = SliderFrame.Minimum.ToString("#");
        }


    }

    [Obsolete]
    private async Task LoadLocation(string loc) //loc can be a file or folder
    {
        if (loc == null || loc == String.Empty)
            return;

        ResetVars();

        if (!IsFolder)
        {
            await Images.FromFile(loc);

        }
        else
        {
            List<string> files = new List<string>();
            try
            {
                foreach (string f in Directory.GetFiles(loc, "*.*", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(f);
                    if(fileName.EndsWith(".dcm", StringComparison.InvariantCultureIgnoreCase) || !fileName.Contains("."))
                        files.Add(f);
                }
            }
            catch (UnauthorizedAccessException) { }

            await Images.FromFiles(files);
        }

        await _loadUI();
    }

    [Obsolete]
    private Task _loadUI()
    {
        Device.BeginInvokeOnMainThread(() =>
        {
            BtnFocusPicker.IsVisible = UI.ImageRoots.Count > 1;

            if (Images.Frames.Count == 0 || Images.Frames[0].Buffer == null)
            {
                //DisplayAlert("ERROR", "Image buffer at index=0 is null.", "OK");
                CanDraw = false;

                GridProgBar.IsVisible = false;
                GridSlider.IsVisible = false;
                GridImport.IsVisible = true;

                DisplayAlert("Failed", "No DICOM or image files found.", "OK");

                return;
            }

            GridProgBar.IsVisible = false;


            //Getting race conditions by binding this to a ViewModel so I will do it the old fashioned way
            PickerRoot.Items.Clear();
            foreach (string root in UI.RootList)
                PickerRoot.Items.Add(root);

            //!this is where I can solve SE000005

            PickerRoot.SelectedIndex = PickerRoot.Items.Count > 0 ? 0 : - 1;
            _rootIdx = PickerRoot.SelectedIndex;


            //
            _loadFromRoot();

            while (Images.Frames[_currentFrame].Buffer == null)
                Thread.Sleep(10);

            GridImport.IsVisible = true;

            CanDraw = true;
            _drawFrame(0);
        });

        return Task.CompletedTask;
    }


    [Obsolete]
    private void ResetVars()
    {
        CanDraw = false;
        _currentFrame = 0;

        Images.Frames.Clear();
        Images = new FrameCollection();

        //Figure out the actual UI width so as to fill/draw in the entire screen
        var mainDisplayInfo = DeviceDisplay.MainDisplayInfo;
        var orientation = mainDisplayInfo.Orientation;
        var rotation = mainDisplayInfo.Rotation;
        var width = mainDisplayInfo.Width;
        var uiWidth = width / mainDisplayInfo.Density;
        DesiredWidth = (float)uiWidth; //ref var

        Device.BeginInvokeOnMainThread(() =>
        {
            GridImport.IsVisible = false;
            GFX.HeightRequest = (int)DeviceDisplay.MainDisplayInfo.Height; //otherwise it remains as 0 regardless if an image is draw on canvas
                                                                           //GFX.WidthRequest = (int)DeviceDisplay.MainDisplayInfo.Width;

            SliderFrame.Minimum = 1.0d;
            SliderFrame.Value = 1.0d;
            SliderFrame.Maximum = 1.0d;

            GridSlider.IsVisible = false;
            GridProgBar.IsVisible = true;

            BtnFocusPicker.IsVisible = false;
        });
    }

    private int _getListIndexOfFrame(int frameNo)
    {
        if(!IsFolder)
        {
            return Images.Frames.FindIndex(frame => 
                frame.Number == frameNo && 
                frame.DicomLoc == Location);
        }
        else
        {
            //!
        }
        return 0;
    }

    private void _drawFrame(int val) //assigns _currentFrame which is in turn used as a ref by the drawing class
    {
        if (!CanDraw)
            return;

        if (val < _totalFrames && val >= 0)
        {
            _currentFrame = val;
            LblFrameNo.Text = $"[{_currentFrame + 1}/{_totalFrames}]"; //+1 to account for arrs being 0 based
            _setInfoModel();
            GFX.Invalidate();
        }
    }


    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (_totalFrames <= 1)
            return; //no frames to pan

        switch (e.StatusType)
        {
            case GestureStatus.Running:
                int xStep = (int)GFX.Width / _totalFrames;
                if (xStep == 0)
                    xStep = 1; //avoid DivByZero exception

                int movedX = (int)e.TotalX;
                int diff = (int)Math.Floor((double)movedX - _startX);

                decimal _framesToSkip = Convert.ToDecimal(diff / xStep);
                int framesToSkip = (int)Math.Floor(_framesToSkip);

                if (framesToSkip >= 1 || framesToSkip <= -1)
                {
                    int newFrame = _currentFrame + (int)framesToSkip;
                    while (newFrame >= _totalFrames || newFrame < 0) //Bounds check
                        newFrame = newFrame > 0 ? newFrame - _totalFrames : _totalFrames + newFrame;

                    if (_currentFrame != newFrame)
                    {
                        _currentFrame = newFrame;
                        SliderFrame.Value = newFrame+1; //we are using 1 == 0 as slider.minimum
                    }

                    _startX = movedX;
                    _startY = (int)e.TotalY;
                }
                break;
            default:
                _startX = 0;
                _startY = 0;
                break;
        }
    }

    private void SliderFrame_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        int val = (int)e.NewValue - 1;
        _drawFrame(val);
    }

    public async Task<FileResult> ImportFile(PickOptions options)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(options);
            return result;
        }
        catch (Exception ex)
        {
        }

        return null;
    }


    private async void BtnImport_Clicked(object sender, EventArgs e)
    {
        GridImport.IsVisible = false;
        await Shell.Current.GoToAsync("//BrowserPage"); //Define uri in AppShell.xaml
        GridImport.IsVisible = true;
    }

    public static int _rootIdx = 0;

    private void PickerRoot_SelectedIndexChanged(object sender, EventArgs e)
    {
        _rootIdx = PickerRoot.SelectedIndex;
        _loadFromRoot();
        _setInfoModel();
        GFX.Invalidate();
    }

    private void BtnFocusPicker_Clicked(object sender, EventArgs e)
    {
        if (PickerRoot.Items.Count>1)
            PickerRoot.Focus();
    }

    private void _resetInfoModel()
    {
        UI.InfoView.Clear();
    }

    private void _setInfoModel()
    {
        if (_totalFrames <= 0 || _currentFrame < -1 || _currentFrame >= _totalFrames || _rootIdx < 0)
        {
            _resetInfoModel();
            return;
        }

        ImageRoot root = UI.ImageRoots[_rootIdx];
        int actualFrameIdx = Images.Frames.FindIndex(f => f.Number == _currentFrame && f.OriginalDirectory == root.FullFolderPath);

        if (actualFrameIdx >= Images.Frames.Count)
        {
            _resetInfoModel();
            return;
        }

        UI.InfoView.PatientInfo = Images.Frames[actualFrameIdx].Info.PatientDetails;
        UI.InfoView.StudyInfo = Images.Frames[actualFrameIdx].Info.StudyDetails;

    }

    public static byte[] GetCurrentFrameBuffer()
    {
        if(_totalFrames <= 0 || _currentFrame < -1 || _currentFrame >= _totalFrames || _rootIdx < 0)
            return null;

        ImageRoot root = UI.ImageRoots[_rootIdx];
        int actualFrameIdx = Images.Frames.FindIndex(f => f.Number == _currentFrame && f.OriginalDirectory == root.FullFolderPath);

        if (actualFrameIdx >= Images.Frames.Count)
            return null;

        return Images.Frames[actualFrameIdx].Buffer;
    }
}



