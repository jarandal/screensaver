using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using ExifLibrary;
using System.Net;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsPresentation;


namespace PictureSlideshowScreensaver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class Screensaver : Window
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;
        private const int MOUSEEVENTF_MOVE = 0x01;
    
        private string _path = null;
        private double _updateInterval = 13.5; // seconds
        private int _fadeSpeed = 3000;      // milliseconds

        private List<string> _images;
        private ITwoWayEnumerator<string> _imageEnum;
        private DispatcherTimer _switchImage;
        private Point _mouseLocation = new Point(0, 0);

        private DispatcherTimer _deleteImage;

        private double panX;
        private double panY;

        private List<Grid> btnArray = new List<Grid>();
                
        Random rand = new Random();

        private System.Drawing.Rectangle _bounds;

        public Screensaver(System.Drawing.Rectangle bounds)
        {

            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\PictureSlideshowScreensaver");
            if (key != null)
            {
                _path = (string)key.GetValue("ImageFolder");
//#if DEBUG
//                _path = @"D:\Prog\Personal\pictureslideshow\PictureSlideshowScreensaver\img";
//#endif
                _updateInterval = double.Parse((string)key.GetValue("Interval"));
            }

            InitializeComponent();

            
            _bounds = bounds;

            panX = bounds.Width * 0.03;
            panY = bounds.Height * 0.03;

            setImagesSizes();
                        
            _images = new List<string>();
            _switchImage = new DispatcherTimer();
            _switchImage.Interval = TimeSpan.FromSeconds(_updateInterval);
            _switchImage.Tick += new EventHandler(_fade_Tick);


            _deleteImage = new DispatcherTimer();
            _deleteImage.Interval = TimeSpan.FromSeconds(2);
            _deleteImage.Tick += new EventHandler(_deleteImage_Tick);
            
            FrameworkElement[] topBtnArray = new FrameworkElement[] { standby, null, null, null, delete,null,null, null, earth_location };
            FrameworkElement[] bottomBtnArray = new FrameworkElement[] { rotate_left, null, null, media_step_back, media_play, media_step_forward, null, null, rotate_right };

            int bW = _bounds.Width / topBtnArray.Length;
            
            int i;

            i = 0;
            foreach (FrameworkElement x in topBtnArray)
            {
                if (x != null)
                {
                    x.Width = bW;
                    x.Height = bW;
                    Canvas.SetLeft(x, bW * i);
                    Canvas.SetTop(x, 0);
                }
                i++;
            }

            i = 0;
            foreach (FrameworkElement x in bottomBtnArray)
            {
                if (x != null)
                {
                    x.Width = bW;
                    x.Height = bW;
                    Canvas.SetLeft(x, bW * i);
                    Canvas.SetTop(x, bounds.Height - bW);
                }
                i++;
            }

            //Canvas.SetLeft(rotate_left, 0);
            //Canvas.SetLeft(media_step_back, bW * 3);
            //Canvas.SetLeft(media_play, bW * 4);
            //Canvas.SetLeft(media_step_forward, bW * 5);
            //Canvas.SetLeft(rotate_right, bW * 8);
            
            //Canvas.SetLeft(standby, 0);
            //Canvas.SetLeft(delete, bW * 4);
            //Canvas.SetLeft(earth_location, bW * 8);
            //Canvas.SetTop(standby, bounds.Height - bW );
            //Canvas.SetTop(delete, bounds.Height - bW);
            //Canvas.SetTop(earth_location, bounds.Height - bW );
        }

        void setImagesSizes() {
            img1.Width = _bounds.Width + panX * 2;
            img2.Width = _bounds.Width + panX * 2;
            img1.Height = _bounds.Height + panY * 2;
            img2.Height = _bounds.Height + panY * 2;

            bkg1.Width = _bounds.Width * 1.4;
            bkg2.Width = _bounds.Width * 1.4;
            bkg1.Height = _bounds.Height * 3;
            bkg2.Height = _bounds.Height * 3;

            Canvas.SetTop(bkg1, -_bounds.Height * 1);
            Canvas.SetTop(bkg2, -_bounds.Height * 1);
            Canvas.SetLeft(bkg1, -_bounds.Width * 0.2);
            Canvas.SetLeft(bkg2, -_bounds.Width * 0.2);
            
            Thickness margin = new Thickness();
            margin.Left = -panX;
            margin.Right = panX;
            margin.Top = -panY;
            margin.Bottom = panY;

            img1.Margin = margin;
            img2.Margin = margin;

            Canvas.SetTop(img1, 0);
            Canvas.SetTop(img2, 0);
            Canvas.SetLeft(img1, 0);
            Canvas.SetLeft(img2, 0);

            img1.Stretch = Stretch.Uniform;
            img2.Stretch = Stretch.Uniform;
        }

        void _fade_Tick(object sender, EventArgs e)
        {
            if (System.Windows.SystemParameters.PowerLineStatus == PowerLineStatus.Online) { 
                mouse_event(MOUSEEVENTF_MOVE, 1, 1, 0, 0);
            }
            NextImage(true);
        }



        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Maximize window
            this.WindowState = System.Windows.WindowState.Maximized;
#if DEBUG
            this.WindowState = System.Windows.WindowState.Normal;
#endif

            // Load images
            if (_path != null)
            {
                if (Directory.Exists(_path))
                {

                    MainMapGrid.Width = _bounds.Width / 7;
                    MainMapGrid.Height = _bounds.Width / 7;

                    MainMap.Width = _bounds.Width / 7;
                    MainMap.Height = _bounds.Width / 7;

                    GMaps.Instance.UseMemoryCache = true;

                    Canvas.SetLeft(MainMapGrid, _bounds.Width - MainMapGrid.Width * 6 / 5);
                    Canvas.SetTop(MainMapGrid, _bounds.Height - MainMapGrid.Width * 6 / 5); 
                    
                    MainMap.MapProvider = GMapProviders.GoogleMap;
                    //GMapProvider.WebProxy =  new WebProxy("127.0.0.1", 3128);
                    
                    MainMap.Zoom = 9;
                                                          
                    //foreach (string s in Directory.GetFiles(_path))
                    foreach (string s in GetFiles(_path))
                    {
                        if (s.ToLower().EndsWith(".jpg") | s.ToLower().EndsWith(".png"))
                        {
                            _images.Add(s);
                        }
                    }
                    _images = RandomizeGenericList(_images);

                    if (_images.Count > 0)
                    {
                        _imageEnum = new TwoWayEnumerator<string>(_images.GetEnumerator());
                        NextImage(true);
                        _switchImage.Start();
                    }
                }
                else
                {
                    lblScreen.Content = "Image folder does not exist! Please run configuration.";
                }
            }
            else
            {
                lblScreen.Content = "Image folder not set! Please run configuration.";
            }
        }

        private void bNext_Click(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            NextImage(false);
            StopAnimation();
        }


        private void bPrev_Click(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            PrevImage(false);
            StopAnimation();
        }

        private void bPlay_Click(object sender, RoutedEventArgs e)
        {
            _fade_Tick(this, null);
            ResumeAnimation();

        }

        private void PrevImage(bool animate)
        {
            if (_imageEnum.MovePrevious())
            {
                CurrentImage(true);
                
            }

        }

        private void NextImage( bool animate )
        {
            if (_imageEnum.MoveNext())
            {
                CurrentImage(animate);
            }

        }

        private void StopAnimation()
        {
            _switchImage.Stop();

            img1.BeginAnimation(Image.OpacityProperty, null);
            img2.BeginAnimation(Image.OpacityProperty, null);
            
            if (lastImageNumber == 1) {
                img1.Opacity = 1;
                img2.Opacity = 0;
            }
            else {
                img1.Opacity = 0;
                img2.Opacity = 1;
            }

            foreach (Image img in (new Image[] { img1, img2 }))
            {
                var group = new TransformGroup();
                TranslateTransform trans = new TranslateTransform();
                group.Children.Add(trans);
                img.RenderTransform = group;
                trans.BeginAnimation(TranslateTransform.XProperty, null);
                trans.BeginAnimation(TranslateTransform.YProperty, null);
                var scale = new ScaleTransform(1.0, 1.0);
                group.Children.Add(scale);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }

        }
        
        private void ResumeAnimation()
        {
            hideButtons();
            _switchImage.Start();
        }

        private void CurrentImage(Boolean animate)
        {
            try
            {
                Image aux;
                lastBitmapImage = FromFile(_imageEnum.Current);

                if (animate)
                     { aux = FadeToImage(lastBitmapImage, _fadeSpeed); } 
                else
                    { aux = FadeToImage(lastBitmapImage, 0); };

                bool gps = false;
                string filename = _imageEnum.Current;

                FileInfo fi = new System.IO.FileInfo(filename);
                FolderName.Content = fi.Directory.Name;

                try
                {
                    if (_imageEnum.Current.ToUpper().EndsWith("JPG"))
                    {
                        ExifFile file = ExifFile.Read(filename);
                        if (file.Properties.Keys.Contains(ExifTag.GPSLatitude) && file.Properties.Keys.Contains(ExifTag.GPSLongitude))
                        {
                            GPSLatitudeLongitude lat = file.Properties[ExifTag.GPSLatitude] as GPSLatitudeLongitude;
                            GPSLatitudeLongitude lng = file.Properties[ExifTag.GPSLongitude] as GPSLatitudeLongitude;

                            GPSLatitudeRef latR = (GPSLatitudeRef)file.Properties[ExifTag.GPSLatitudeRef].Value;
                            GPSLongitudeRef lngR = (GPSLongitudeRef)file.Properties[ExifTag.GPSLongitudeRef].Value;

                            int NS = 1; if (latR == GPSLatitudeRef.South) { NS = -1; }
                            int WE = 1; if (lngR == GPSLongitudeRef.West) { WE = -1; }

                            if (lat != null && lng != null)
                            {
                                if (animate)
                                    FadeIn(MainMapGrid);
                                else
                                    MainMapGrid.Opacity = 1;

                                MainMap.Position = new PointLatLng(NS * (double)lat.ToFloat(), WE * (double)lng.ToFloat());
                                MainMap.Markers.Clear();
                                MainMap.ReloadMap();

                                gps = true;

                            }
                        }
                    }
                }
                catch (Exception) { }

                if (!gps)
                {
                    if (animate)
                        FadeOut(MainMapGrid);
                    else
                        MainMapGrid.Opacity = 0;
                }

                MoveTo(aux, 0, 0);

                return;
            }
            catch (Exception)
            {
                _imageEnum.MoveNext();
                return;
            }
        }

        int lastImageNumber = 1;

        private Image FadeToImage(BitmapImage img, int fadeSpeed )
        {
            Image result = null;
            DoubleAnimation da1;
            DoubleAnimation da2;

            WriteableBitmap wb = new WriteableBitmap(img);
            int w =  Convert.ToInt32(img.Width / 7);
            int h = Convert.ToInt32(img.Height / 7);
            wb = wb.Resize(w, h, WriteableBitmapExtensions.Interpolation.Bilinear);
            wb = wb.Convolute(WriteableBitmapExtensions.KernelGaussianBlur5x5);

            if (lastImageNumber == 2)
            {

                Canvas.SetTop(img1, 0);
                Canvas.SetLeft(img1, 0);

                img1.Source = img;
                bkg1.Source = wb;

                setImagesSizes();
                
                da1 = new DoubleAnimation(1, TimeSpan.FromMilliseconds(fadeSpeed));
                da2 = new DoubleAnimation(0, TimeSpan.FromMilliseconds(fadeSpeed));

                img1.BeginAnimation(Image.OpacityProperty, da1);
                img2.BeginAnimation(Image.OpacityProperty, da2);

                //bkg1.Opacity = 1;
                //bkg2.Opacity = 0;

                bkg1.BeginAnimation(Image.OpacityProperty, da1);
                bkg2.BeginAnimation(Image.OpacityProperty, da2);

                result = img1;
                lastImageNumber = 1;
            }
            else if (lastImageNumber == 1)
            {

                Canvas.SetTop(img2, 0);
                Canvas.SetLeft(img2, 0);

                img2.Source = img;
                bkg2.Source = wb;

                setImagesSizes();

                da1 = new DoubleAnimation(0, TimeSpan.FromMilliseconds(fadeSpeed));
                da2 = new DoubleAnimation(1, TimeSpan.FromMilliseconds(fadeSpeed));

                img1.BeginAnimation(Image.OpacityProperty, da1);
                img2.BeginAnimation(Image.OpacityProperty, da2);

                //bkg1.Opacity = 0;
                //bkg2.Opacity = 1;
                bkg1.BeginAnimation(Image.OpacityProperty, da1);
                bkg2.BeginAnimation(Image.OpacityProperty, da2);

                result = img2;
                lastImageNumber = 2;
            }
            return result;
        }

        private static Size ScaleSize(Size from, int? maxWidth, int? maxHeight)
        {
            if (!maxWidth.HasValue && !maxHeight.HasValue) throw new ArgumentException("At least one scale factor (toWidth or toHeight) must not be null.");
            if (from.Height == 0 || from.Width == 0) throw new ArgumentException("Cannot scale size from zero.");

            double? widthScale = null;
            double? heightScale = null;

            if (maxWidth.HasValue)
            {
                widthScale = maxWidth.Value / (double)from.Width;
            }
            if (maxHeight.HasValue)
            {
                heightScale = maxHeight.Value / (double)from.Height;
            }

            double scale = Math.Max((double)(widthScale ?? heightScale),
                                     (double)(heightScale ?? widthScale));

            return new Size((int)Math.Floor(from.Width * scale), (int)Math.Ceiling(from.Height * scale));
        }

        private void FadeIn(FrameworkElement g, int miliseconds)
        {
            
            DoubleAnimation da1;

            da1 = new DoubleAnimation(1, TimeSpan.FromMilliseconds(miliseconds));
            g.BeginAnimation(FrameworkElement.OpacityProperty, da1);
            
        }

        private void FadeIn(FrameworkElement g)
        {
            FadeIn(g, _fadeSpeed);
            
        }

        private void FadeOut(FrameworkElement g, int miliseconds)
        {

            DoubleAnimation da1;

            da1 = new DoubleAnimation(0, TimeSpan.FromMilliseconds(miliseconds));
            g.BeginAnimation(FrameworkElement.OpacityProperty, da1);

        }

        private void FadeOut(FrameworkElement g)
        {

            FadeOut(g, _fadeSpeed);
        }

        private bool rndBool()
        {
            int r = rand.Next(0, 2);
            if (r == 1) { return true; } else { return false; };
        }

        private void MoveTo(Image target, double newX, double newY)
        {

            int bX = rndBool() ? 1 : -1;
            int bY = rndBool() ? 1 : -1;

            Canvas.SetLeft(target, 0);
            Canvas.SetTop(target, 0);

            var top = Canvas.GetTop(target); 
            var left = Canvas.GetLeft(target);

            var group = new TransformGroup();

            TranslateTransform trans = new TranslateTransform();
            group.Children.Add(trans);
            target.RenderTransform = group;
            DoubleAnimation anim1 = new DoubleAnimation(top, newY + panY * bY, TimeSpan.FromSeconds(_updateInterval + _fadeSpeed / 1000));
            DoubleAnimation anim2 = new DoubleAnimation(left, newX + panX * bX, TimeSpan.FromSeconds(_updateInterval + _fadeSpeed / 1000));
            trans.BeginAnimation(TranslateTransform.XProperty, anim1);
            trans.BeginAnimation(TranslateTransform.YProperty, anim2);


            var scale = new ScaleTransform(1.0, 1.0);
            group.Children.Add(scale);

            DoubleAnimation danim1 = new DoubleAnimation(1, 1 + (0.05 * bX), TimeSpan.FromSeconds(_updateInterval + _fadeSpeed / 1000));
            DoubleAnimation danim2 = new DoubleAnimation(1, 1 + (0.05 * bX), TimeSpan.FromSeconds(_updateInterval + _fadeSpeed / 1000));

            scale.BeginAnimation(ScaleTransform.ScaleXProperty,danim1);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty,danim2);
            
        }

        public static List<T> RandomizeGenericList<T>(IList<T> originalList)
        {
            List<T> randomList = new List<T>();
            Random random = new Random();
            T value = default(T);

            //now loop through all the values in the list
            while (originalList.Count() > 0)
            {
                //pick a random item from th original list
                var nextIndex = random.Next(0, originalList.Count());
                //get the value for that random index
                value = originalList[nextIndex];
                //add item to the new randomized list
                randomList.Add(value);
                //remove value from original list (prevents
                //getting duplicates
                originalList.RemoveAt(nextIndex);
            }

            //return the randomized list
            return randomList;
        }

        private void bExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        Boolean isDeleteEnabled = false;
        private void bDelete_Click(object sender, RoutedEventArgs e)
        {
            if (!isDeleteEnabled) { 
                Color col = (Color)ColorConverter.ConvertFromString("Red"); 
                delete_img.Background = new SolidColorBrush(col);
                isDeleteEnabled = true;
                _deleteImage.Start();
            }
            else
            {
                isDeleteEnabled = false;
                System.IO.File.Delete(_imageEnum.Current);
                _deleteImage_Tick(this, null);
                _imageEnum.RemoveCurrent();
                NextImage(false);
                ResumeAnimation();
            }
        }

        void _deleteImage_Tick(object sender, EventArgs e)
        {
            Color col = (Color)ColorConverter.ConvertFromString("White");
            delete_img.Background = new SolidColorBrush(col);
            isDeleteEnabled = false;
            _deleteImage.Stop();
            ResumeAnimation();
        }
                
        private void lblScreen_MouseMove(object sender, MouseEventArgs e)
        {
            Point newPos = e.GetPosition(this);
            System.Drawing.Point p = new System.Drawing.Point((int)newPos.X, (int)newPos.Y);
            if ((_mouseLocation.X != 0 & _mouseLocation.Y != 0) & ((p.X >= 0 & p.X <= _bounds.Width) & (p.Y >= 0 & p.Y <= _bounds.Height)))
            {
                if (Math.Abs(_mouseLocation.X - newPos.X) > 10 || Math.Abs(_mouseLocation.Y - newPos.Y) > 10)
                {
#if !DEBUG
                //Application.Current.Shutdown();
#endif
                }
            }

            _mouseLocation = newPos;
        }

        Boolean btnVisible = false;

        private void switchButtons()
        {
            if (btnVisible)
            {
                hideButtons();
                ResumeAnimation();
            }
            else
            {
                showButtons();
                StopAnimation();
            }
        }

        private void lblScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            switchButtons();
  
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switchButtons();
        }

        private void showButtons()
        {
            btnVisible = true;
            FadeIn(cnvButtons,500);
        }

        private void hideButtons()
        {
            btnVisible = false;
            FadeOut(cnvButtons,500);
        }

        static IEnumerable<string> GetFiles(string path)
        {
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(path);
            while (queue.Count > 0)
            {
                path = queue.Dequeue();
                DirectoryInfo di = new DirectoryInfo(path);
                if (!di.Name.StartsWith("."))
                {
                    try
                    {
                        foreach (string subDir in Directory.GetDirectories(path))
                        {
                            queue.Enqueue(subDir);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                    string[] files = null;
                    try
                    {
                        files = Directory.GetFiles(path);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine(ex);
                    }
                    if (files != null)
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            yield return files[i];
                        }
                    }
                }
            }
        }


        private void bRotate_left_Click(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            rotate(-90);
            CurrentImage(false);
            ResumeAnimation();
        }

        private void bRotate_right_Click(object sender, RoutedEventArgs e)
        {
            StopAnimation();
            rotate(90);
            CurrentImage(false);
            ResumeAnimation();
        }

        public void rotate(int degrees)
        {
            BitmapImage img =  lastBitmapImage;
            WriteableBitmap wb = new WriteableBitmap(img);
            wb = wb.RotateFree(degrees, false);
            SaveBitmap(_imageEnum.Current, wb);
        }

        void SaveBitmap(string filename, BitmapSource image5)
        {
            if (filename != string.Empty)
            {
                using (FileStream stream5 = new FileStream(filename, FileMode.Create))
                {
                    JpegBitmapEncoder encoder5 = new JpegBitmapEncoder();
                    encoder5.Frames.Add(BitmapFrame.Create(image5));
                    encoder5.Save(stream5);
                    stream5.Close();
                }
            }
        }

        BitmapImage lastBitmapImage = null;

        public static BitmapImage FromFile(string path)
        {

            System.Drawing.Image img;
            using (var bmpTemp = new System.Drawing.Bitmap(path))
            {
                img = new System.Drawing.Bitmap(bmpTemp);
            }

            BitmapImage bitmapImage = null;
            using (MemoryStream memory = new MemoryStream())
            {
                img.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;
                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
            }
            return bitmapImage;


            //BitmapImage imageSource  = null;
            //MemoryStream ms ;
            //var bytes = File.ReadAllBytes(path);
            //try
            //{
            //    ms = new MemoryStream(bytes);
            //    imageSource = new BitmapImage();
            //    imageSource.BeginInit();
            //    imageSource.StreamSource = ms;
            //    imageSource.EndInit();
            //    ms.Close();
            //}
            //catch (Exception) { }
            //return imageSource;

            //BitmapImage imageSource = new BitmapImage(new Uri(path));
            //return imageSource;

        }

        //metodo supuestamente mas rapido, pero inexacto
        //public System.Drawing.Image RotateImage(System.Drawing.Image img, float rotationAngle)
        //{
        //    // When drawing the returned image to a form, modify your points by 
        //    // (-(img.Width / 2) - 1, -(img.Height / 2) - 1) to draw for actual co-ordinates.

        //    //create an empty Bitmap image 
        //    System.Drawing.Bitmap bmp = new System.Drawing.Bitmap((int)(img.Width * 2), (int)(img.Height * 2));

        //    //turn the Bitmap into a Graphics object
        //    System.Drawing.Graphics gfx = System.Drawing.Graphics.FromImage(bmp);

        //    //set the point system origin to the center of our image
        //    gfx.TranslateTransform((float)bmp.Width / 2, (float)bmp.Height / 2);

        //    //now rotate the image
        //    gfx.RotateTransform(rotationAngle);

        //    //move the point system origin back to 0,0
        //    gfx.TranslateTransform(-(float)bmp.Width / 2, -(float)bmp.Height / 2);

        //    //set the InterpolationMode to HighQualityBicubic so to ensure a high
        //    //quality image once it is transformed to the specified size
        //    gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

        //                //draw our new image onto the graphics object with its center on the center of rotation
        //    System.Drawing.PointF p = new System.Drawing.PointF( (int)(img.Width / 2),  (int)(img.Height / 2));
        //    gfx.DrawImage(img, p);

        //    //dispose of our Graphics object
        //    gfx.Dispose();

        //    //return the image
        //    return bmp;
        //}
     
    }
}
