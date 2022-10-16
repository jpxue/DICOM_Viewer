using System.Collections.ObjectModel;
using System.Reflection;

namespace MAUI_DICOM_Viewer.Pages;

public partial class BrowserPage : ContentPage
{
    string _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public ObservableCollection<FileFolderView> DirInfo { get; } = new() { };

    public BrowserPage()
    {
        InitializeComponent();
        Initialize();
    }

    private async Task Initialize()
    {
        LstView.ItemsSource = DirInfo; //set the UI binding

#if ANDROID
        //Request Read/Write Permissions
        PermissionStatus statusread = await Permissions.RequestAsync<Permissions.StorageRead>();
        PermissionStatus statuswrite = await Permissions.RequestAsync<Permissions.StorageWrite>();
        _currentPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;
#else
        _currentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#endif

        _loadLstView(_currentPath);
    }

    private void _loadLstView(string path)
    {
        try
        {
            LstView.BeginRefresh();

            _prevTappedLoc = null;
            _prevTappedIsFolder = false;

            var dirs = Directory.GetDirectories(path);
            var files = Directory.GetFiles(path);

            if (dirs.Length + files.Length <= 0)
                return;

            DirInfo.Clear();

            foreach (var dir in dirs)
                DirInfo.Add(new FileFolderView(dir, true));
            foreach (var file in files)
                DirInfo.Add(new FileFolderView(file, false));

            _currentPath = path;
            LblTitle.Text = path;

            string parentDir = Directory.GetParent(path).ToString();
            BtnBack.IsVisible = parentDir != path;
        }
        catch (UnauthorizedAccessException)
        {
            DisplayAlert("Insufficient Priviledges", $"Unauthorized access to {path}", "Ok");
            BtnBack.IsVisible = false;
        }
        catch(TargetInvocationException)
        {
            DisplayAlert("Insufficient Permissions", $"Read/Write permissions have not been granted.", "Ok");
            BtnBack.IsVisible = false;
        }
        finally
        {
            LstView.EndRefresh();
        }
    }


    private string _prevTappedLoc = string.Empty;
    private bool _prevTappedIsFolder = false;
    private void LstView_ItemTapped(object sender, ItemTappedEventArgs e)
    {
        FileFolderView selectedItem = LstView.SelectedItem as FileFolderView;

        //Update IsSelected Property
        for (int i = 0; i < DirInfo.Count; i++)
        {
            bool isSelected = DirInfo[i].Location == selectedItem.Location;
            if (DirInfo[i].IsSelected != isSelected)
                DirInfo[i] = new FileFolderView(DirInfo[i].Location, DirInfo[i].IsFolder, isSelected);
        }

        //If Double Tapped -> Expand Folder
        if (_prevTappedLoc == selectedItem.Location)
        {
            if (selectedItem.IsFolder && selectedItem.HasChildren)
                _loadLstView(selectedItem.Location);
        }
        _prevTappedLoc = selectedItem.Location;
        _prevTappedIsFolder = selectedItem.IsFolder;
    }

    private void BtnBack_Clicked(object sender, EventArgs e)
    {
        string parentDir = Directory.GetParent(_currentPath).ToString();
        _loadLstView(parentDir);
    }

    private async void BtnSelected_Clicked(object sender, EventArgs e)
    {
        string loc = _prevTappedLoc;
        if (_prevTappedIsFolder && !_prevTappedLoc.EndsWith("/"))
            loc = string.Concat(_prevTappedLoc, "/");
            
        await Shell.Current.GoToAsync($"//ViewerPage?Location={loc}");
    }


    //await Shell.Current.GoToAsync($"//ViewerPage?FileLoc={file.Location}");
}
