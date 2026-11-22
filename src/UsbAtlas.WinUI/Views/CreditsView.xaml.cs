using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace UsbAtlas.WinUI.Views;

public sealed partial class CreditsView : UserControl
{
    private const string RepoUrl = "https://github.com/porserx/usb-atlas";

    public CreditsView()
    {
        InitializeComponent();
        DataContext = App.ViewModel;
        Loaded += CreditsView_Loaded;
    }

    private void CreditsView_Loaded(object sender, RoutedEventArgs e)
    {
        VersionText.Text = $"v{App.ViewModel.AppVersion}";
        LicenseBody.Text = LoadLicenseText();
    }

    private static string LoadLicenseText()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "LICENSE");
            if (File.Exists(path))
                return File.ReadAllText(path);
        }
        catch
        {
            // Fall through to the embedded copy.
        }

        return "MIT License\n\nCopyright (c) 2026 porserx & EXET\n\n" +
               "Permission is hereby granted, free of charge, to any person obtaining a copy " +
               "of this software and associated documentation files (the \"Software\"), to deal " +
               "in the Software without restriction, including without limitation the rights " +
               "to use, copy, modify, merge, publish, distribute, sublicense, and/or sell " +
               "copies of the Software, and to permit persons to whom the Software is furnished " +
               "to do so, subject to the following conditions:\n\n" +
               "The above copyright notice and this permission notice shall be included in all " +
               "copies or substantial portions of the Software.\n\n" +
               "THE SOFTWARE IS PROVIDED \"AS IS\", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR " +
               "IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, " +
               "FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.";
    }

    private async void Repo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(RepoUrl));
        }
        catch
        {
            // The repository URL is shown in the page if launch is unavailable.
        }
    }

    private async void SourceZip_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "USB-Atlas-source.zip");
            if (File.Exists(path))
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                await Windows.System.Launcher.LaunchFileAsync(file);
                return;
            }
        }
        catch
        {
            // Fall back to the repository link.
        }

        Repo_Click(sender, e);
    }
}
