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

namespace PictureSlideshowScreensaver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Screensaver : Window
    {
        private string _path = null;
        private double _updateInterval = 13.5; // seconds
        private int _fadeSpeed = 3000;      // milliseconds

        private List<string> _images;
        private IEnumerator<string> _imageEnum;
        private DispatcherTimer _switchImage;
        private Point _mouseLocation = new Point(0, 0);

        private double panX;
        private double panY;

        Random rand = new Random();

        private System.Drawing.Rectangle _bounds;

        public Screensaver(System.Drawing.Rectangle bounds)
        {

            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\PictureSlideshowScreensaver");
            if (key != null)
            {
                _path = (string)key.GetValue("ImageFolder");
#if DEBUG 
                _path = @"D:\Prog\Personal\pictureslideshow\PictureSlideshowScreensaver\img";
#endif
                _updateInterval = double.Parse((string)key.GetValue("Interval"));
            }

            InitializeComponent();

            _bounds = bounds;

            panX = bounds.Width * 0.03;
            panY = bounds.Height * 0.03;
                       
            img1.Width = bounds.Width + panX * 2;
            img2.Width = bounds.Width + panX * 2;
            img1.Height = bounds.Height + panY * 2;
            img2.Height = bounds.Height + panY * 2; 

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
                        
            _images = new List<string>();
            _switchImage = new DispatcherTimer();
            _switchImage.Interval = TimeSpan.FromSeconds(_updateInterval);
            _switchImage.Tick += new EventHandler(_fade_Tick);

        }

        void _fade_Tick(object sender, EventArgs e)
        {
            NextImage();
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
                    foreach (string s in Directory.GetFiles(_path))
                    {
                        if (s.ToLower().EndsWith(".jpg") | s.ToLower().EndsWith(".png"))
                        {
                            _images.Add(s);
                        }
                    }
                    _images = RandomizeGenericList(_images);

                    if (_images.Count > 0)
                    {
                        _imageEnum = _images.GetEnumerator();
                        NextImage();
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
            NextImage();
        }

        private void NextImage()
        {
            if (_imageEnum.MoveNext())
            {
                try
                {
                    Image aux = FadeToImage(new BitmapImage(new Uri(_imageEnum.Current)));
                    MoveTo(aux, 0, 0);
                    return;
                }
                catch (Exception ex)
                {
                    //MessageBox.Show("ERROR: " + ex.Message);
                    _imageEnum.MoveNext();
                    return;
                }
            }

            _images = RandomizeGenericList(_images);
            _imageEnum = _images.GetEnumerator();
        }

        private Image FadeToImage(BitmapImage img)
        {
            Image result = null; 
            DoubleAnimation da1;
            DoubleAnimation da2;
            if (img1.Opacity == 0)
            {
                img1.Source = img;
                
                da1 = new DoubleAnimation(1, TimeSpan.FromMilliseconds(_fadeSpeed));
                da2 = new DoubleAnimation(0, TimeSpan.FromMilliseconds(_fadeSpeed));

                img1.BeginAnimation(Image.OpacityProperty, da1);
                img2.BeginAnimation(Image.OpacityProperty, da2);
               
                result = img1;
            }
            else if (img2.Opacity == 0)
            {
                img2.Source = img;

                da1 = new DoubleAnimation(0, TimeSpan.FromMilliseconds(_fadeSpeed));
                da2 = new DoubleAnimation(1, TimeSpan.FromMilliseconds(_fadeSpeed));

                img1.BeginAnimation(Image.OpacityProperty, da1);
                img2.BeginAnimation(Image.OpacityProperty, da2);
                result = img2;
            }
            return result;
        }

        private bool rndBool()
        {
            int r = rand.Next(0, 2);
            if (r == 1) { return true; } else { return false; };
        }

        private void MoveTo(Image target, double newX, double newY)
        {

            int bX =  rndBool()? 1 : -1;
            int bY =  rndBool() ? 1 : -1;

            Canvas.SetLeft(target,0);
            Canvas.SetTop(target,0);

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



            //// resize
            //Storyboard storyboard = new Storyboard();

            //ScaleTransform scale = new ScaleTransform(1.0, 1.0);
            //target.RenderTransformOrigin = new Point(0.5, 0.5);
            //target.RenderTransform = scale;

            //DoubleAnimation growAnimation = new DoubleAnimation();
            //growAnimation.Duration = TimeSpan.FromSeconds(_updateInterval);
            //growAnimation.From = 1;
            //growAnimation.To = 1.8;
            //storyboard.Children.Add(growAnimation);

            //DoubleAnimation growAnimation2 = new DoubleAnimation();
            //growAnimation.Duration = TimeSpan.FromSeconds(_updateInterval);
            //growAnimation.From = 1;
            //growAnimation.To = 1.8;
            //storyboard.Children.Add(growAnimation2);

            //Storyboard.SetTargetProperty(growAnimation, new PropertyPath("RenderTransform.ScaleX"));
            //Storyboard.SetTarget(growAnimation, target);

            //Storyboard.SetTargetProperty(growAnimation2, new PropertyPath("RenderTransform.ScaleY"));
            //Storyboard.SetTarget(growAnimation2, target);

            //storyboard.Begin();
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

        private void lblScreen_MouseMove(object sender, MouseEventArgs e)
        {
            Point newPos = e.GetPosition(this);
            System.Drawing.Point p = new System.Drawing.Point((int)newPos.X, (int)newPos.Y);
            if ((_mouseLocation.X != 0 & _mouseLocation.Y != 0) & ((p.X >= 0 & p.X <= _bounds.Width) & (p.Y >= 0 & p.Y <= _bounds.Height)))
            {
                if (Math.Abs(_mouseLocation.X - newPos.X) > 10 || Math.Abs(_mouseLocation.Y - newPos.Y) > 10)
                {
#if !DEBUG
                    Application.Current.Shutdown();
#endif
                }
            }

            _mouseLocation = newPos;
        }

        private void lblScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
#if !DEBUG 
            Application.Current.Shutdown();
#endif
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
#if !DEBUG 
            Application.Current.Shutdown();
#endif
        }
    }
}
