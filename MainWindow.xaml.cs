using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PaintGC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private short _color = 0b00100000000;
        private short _mode = 0b00000000001;


        public short ColorMode 
        { 
            get 
            { 
                return (short)(_color | _mode); 
            } 
            set 
            { 
                _color = (short)(value & colorMask);
                _mode = (short)(value & modeMask);
            } 
        }
        private readonly short modeMask = 0b00000111111;
        private readonly short colorMask = 0b11100000000;

        private readonly Brush[] brushPalette = new Brush[]
        {
            new SolidColorBrush(Color.FromRgb(255, 0, 0)),
            new SolidColorBrush(Color.FromRgb(0, 255, 0)),
            new SolidColorBrush(Color.FromRgb(0, 0, 255))
        };

        private short currentBrush = 0;
        private Point startPosition, finalPosition;
        private TextBlock? currentTextBlock;
        private Shape? currentShape;
        private List<Shape> currentShapes = new List<Shape>();
        private UIElement? currentDragElement;

        private Brush GetBrushColor(short brush)
        {
            switch (brush & colorMask)
            {
                case 1024:
                    return brushPalette[0];
                case 512:
                    return brushPalette[1];
                case 256:
                    return brushPalette[2];
                default:
                    return brushPalette[0];
            }
        }

        private Shape GetBrush(short brush)
        {
            switch(brush & modeMask)
            {
                case 1: // rectangle
                     return new Rectangle() { Stroke = GetBrushColor(brush), StrokeThickness = 0, Fill = GetBrushColor(brush) };

                case 2: // ellipse
                     return new Ellipse() { Stroke = GetBrushColor(brush), StrokeThickness = 0, Fill = GetBrushColor(brush) };

                case 4: //triangle
                    return new Polygon() { Stroke = GetBrushColor(brush), StrokeThickness = 0, Fill = GetBrushColor(brush) };

                case 8: //line
                    return new Line() { Stroke = GetBrushColor(brush), StrokeThickness = 4, Fill = GetBrushColor(brush) };

                case 16: //pencil
                    return new Line() { Stroke = GetBrushColor(brush), StrokeThickness = 4, Fill = GetBrushColor(brush) };

                case 32: //text
                default:
                    return new Rectangle() { Stroke = GetBrushColor(brush), StrokeThickness = 0, Fill = GetBrushColor(brush) };

            }
        }

        private void TextBlockWriter(object sender, KeyEventArgs e)
        {
            if(_mode == 32 && currentTextBlock != null)
            {
                currentTextBlock.Text += e.Key.ToString().ToLower();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void PickBrushMode(object sender, RoutedEventArgs e)
        {
            var pressedButton = sender as Button;
            if (pressedButton == null)
                throw new NullReferenceException();
            _mode = short.Parse(pressedButton.Tag.ToString());
            DataLabel.Content = $"Mode: {_mode} Color: {_color} StartLoc: x: {startPosition.X} y: {startPosition.Y}";
        }

        private void PickBrushColor(object sender, RoutedEventArgs e)
        {
            var pressedButton = sender as Button;
            if (pressedButton == null)
                throw new NullReferenceException();
            _color = short.Parse(pressedButton.Tag.ToString());
            DataLabel.Content = $"Mode: {_mode} Color: {_color} StartLoc: x: {startPosition.X} y: {startPosition.Y}";
        }

        private void Canva_MouseMove(object sender, MouseEventArgs e)
        {
            // text
            if(!Canva.IsMouseCaptured || currentShape == null)
                return;

            Point currentLocation = e.MouseDevice.GetPosition(Canva);

            if(_mode == 16) // pencil
            {
                return;
            }

            if(_mode == 8) // line
            {
                ((Line)currentShape).X1 = startPosition.X;
                ((Line)currentShape).Y1 = startPosition.Y;
                ((Line)currentShape).X2 = currentLocation.X;
                ((Line)currentShape).Y2 = currentLocation.Y;
                return;
            }

            if(_mode == 4) // triangle
            {
                ((Polygon)currentShape).Points = new PointCollection(
                    new Point[] {
                        new Point(startPosition.X, startPosition.Y),
                        new Point(currentLocation.X, currentLocation.Y),
                        new Point(startPosition.X, currentLocation.Y)
                    }
                );
                return;
            }
            if (_mode == 2 || _mode == 1)
            {

                double minX = Math.Min(currentLocation.X, startPosition.X);
                double minY = Math.Min(currentLocation.Y, startPosition.Y);
                double maxX = Math.Max(currentLocation.X, startPosition.X);
                double maxY = Math.Max(currentLocation.Y, startPosition.Y);

                Canvas.SetTop(currentShape, minY);
                Canvas.SetLeft(currentShape, minX);


                double h = maxY - minY;
                double w = maxX - minX;
                ((Shape)currentShape).Height = Math.Abs(h);
                ((Shape)currentShape).Width = Math.Abs(w);
            }
         
            DataLabel.Content = $"Mode: {_mode} Color: {_color} StartLoc: x: {startPosition.X} y: {startPosition.Y} CurrentLoc: x: {currentLocation.X} y: {currentLocation.Y}";

        }

        private void SaveCanvas(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "PNG File (*.png)|*.png|JPEG File (*.jpeg)|*.jpeg";
            sfd.ValidateNames = true;

            if (sfd.ShowDialog() == true)
            {
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)Canva.ActualWidth, (int)Canva.ActualHeight, 96d, 96d, PixelFormats.Default);
                rtb.Render(Canva);

                switch (sfd.FilterIndex)
                {
                    case 1: //png
                        BitmapEncoder pngEncoder = new PngBitmapEncoder();
                        pngEncoder.Frames.Add(BitmapFrame.Create(rtb));

                        using (var fs = File.OpenWrite(sfd.FileName))
                        {
                            pngEncoder.Save(fs);
                        }
                        break;
                    case 2: //jpeg
                        BitmapEncoder jpegEncoder = new JpegBitmapEncoder();
                        jpegEncoder.Frames.Add(BitmapFrame.Create(rtb));

                        using (var fs = File.OpenWrite(sfd.FileName))
                        {
                            jpegEncoder.Save(fs);
                        }
                        break;
                    default:
                        return;
                }
            }
        }

        private void Canva_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Canva.ReleaseMouseCapture();
            currentShape = null;
        }

        private void Canva_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Canva.CaptureMouse();

            startPosition = finalPosition = e.MouseDevice.GetPosition(Canva);
            currentBrush = (short)(_color | _mode);
            currentShape = GetBrush(currentBrush);
            if (_mode == 32)
            {
                currentTextBlock = new TextBlock()
                {
                    FontSize = 24,
                    Foreground = GetBrushColor(currentBrush)
                };
                Canvas.SetTop(currentTextBlock, startPosition.Y);
                Canvas.SetLeft(currentTextBlock, startPosition.X);
                Canva.Children.Add(currentTextBlock);
                currentShape = null;
                return;
            }
            if (_mode == 16)
            {
                ((Line)currentShape).X1 = startPosition.X;
                ((Line)currentShape).Y1 = startPosition.Y;
                ((Line)currentShape).X2 = startPosition.X + 2;
                ((Line)currentShape).Y2 = startPosition.Y + 2;
            }

            currentTextBlock = null;

            
            Canva.Children.Add(currentShape);
            currentShapes.Add(currentShape);

            currentDragElement = currentShapes[currentShapes.Count - 1];
            DataLabel.Content = $"Mode: {_mode} Color: {_color} StartLoc: x: {startPosition.X} y: {startPosition.Y}";
        }
    }
}
