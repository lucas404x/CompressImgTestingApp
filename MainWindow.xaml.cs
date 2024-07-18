using ImageMagick;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Windows;

namespace CompressImgTestingApp;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string SizeComparerText = "The size was {0} kb. The new one's {1} kb.";

    private readonly ImageOptimizer _optimizer = new() { OptimalCompression = true };

    public MainWindow()
    {
        InitializeComponent();
        MagickNET.Initialize();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        string? filePath = lblSelectedFilePath.Content.ToString();
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            long originalSize = new FileInfo(filePath).Length;
            string compressedFilePath = await Compress(filePath);
            long compressedSize = new FileInfo(compressedFilePath).Length;

            lblSizeComparer.Content = string.Format(SizeComparerText, originalSize / 1000, compressedSize / 1000);
            lblSizeComparer.Visibility = Visibility.Visible;

            Process.Start("explorer.exe", compressedFilePath);
        }
    }

    public async Task<string> Compress(string filePath)
    {
        MagickNET.Initialize();
        if (!File.Exists(filePath)) return string.Empty;

        var dir = Directory.CreateDirectory(Path.Join(Path.GetTempPath(), nameof(Compress)));
        Debug.Print($"{dir.FullName}");

        using (MagickImage image = new MagickImage(filePath))
        {
            string imgPath = dir.FullName;
            string fileHash = await GenerateImgHash(filePath);

            if (_optimizer.IsSupported(filePath))
            {
                imgPath = Path.Join(imgPath, $"output_{fileHash}.{Path.GetExtension(filePath)}");
                await image.WriteAsync(imgPath);

                _optimizer.OptimalCompression = true;
                _optimizer.Compress(imgPath);
            }
            else
            {
                imgPath = Path.Join(imgPath, $"output_{fileHash}.jpg");
                image.Format = MagickFormat.Jpg;
                await image.WriteAsync(imgPath);
            }

            return imgPath;
        }
    }

    private static async Task<string> GenerateImgHash(string filePath)
    {
        byte[] hashData = SHA256.HashData(await File.ReadAllBytesAsync(filePath));
        return BitConverter.ToString(hashData)
            .Replace("-", string.Empty)
            .ToLower();
    }

    private void Button_Click_1(object sender, RoutedEventArgs e)
    {
        // Cria uma nova instância do OpenFileDialog
        OpenFileDialog openFileDialog = new()
        {
            Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp;*.bmp",
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
        };

        if (openFileDialog.ShowDialog() == true)
        {
            lblSelectedFilePath.Content = openFileDialog.FileName;
        }
    }
}