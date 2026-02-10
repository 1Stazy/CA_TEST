using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls; // To jest WPF
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using CinemaSystem.Desktop.Models;
using System.Windows.Input;

// Aliasy dla typów, które mogą się gryźć z WinForms
using Color = System.Windows.Media.Color;
using Brushes = System.Windows.Media.Brushes;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

namespace CinemaSystem.Desktop.Views.Controls
{
    /// <summary>
    /// Kontrol karuzelek 3D (efekt CoverFlow) - wyswietlanie filmów w 3D.
    /// </summary>
    /// <remarks>
    /// Użęcie: Rotacja poprzez mysę, klawiaturę, a lub programowo.
    /// Zaznaczony film znajduje się w środku sceny z większym rozmiam.
    /// Sczym i prawy z animacją zmniejszania/zwiększania podczas rotacji.
    /// </remarks>
    // ▼▼▼ ZMIANA TUTAJ: Jawnie wskazujemy System.Windows.Controls.UserControl ▼▼▼
    public partial class Carousel3DView : System.Windows.Controls.UserControl
    {
        private readonly Dictionary<Film, GeometryModel3D> _filmModels = new();
        private readonly Model3DGroup _group = new();
        
        /// <summary>
        /// Property odległości kamery od sceny 3D (zoom).
        /// </summary>
        public static readonly DependencyProperty CameraDistanceProperty =
        DependencyProperty.Register("CameraDistance", typeof(double), typeof(Carousel3DView),
        new PropertyMetadata(12.0, OnCameraDistanceChanged));

        /// <summary>
        /// Pobiera lub ustawia odległość kamery (zoom efekt).
        /// </summary>
        public double CameraDistance
        {
            get => (double)GetValue(CameraDistanceProperty);
            set => SetValue(CameraDistanceProperty, value);
        }
        
        /// <summary>
        /// Obsługuje zmianę odległości kamery.
        /// </summary>
        private static void OnCameraDistanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Carousel3DView)d;
            // Znajdujemy kamerę w XAML po nazwie "Camera" (upewnij się, że tak się nazywa w XAML)
            if (control.FindName("Camera") is System.Windows.Media.Media3D.PerspectiveCamera camera)
            {
                // Ustawiamy nową pozycję Z, zachowując X i Y
                camera.Position = new System.Windows.Media.Media3D.Point3D(0, 0, (double)e.NewValue);
            }
        }
        /// <summary>
        /// Inicjalizuje karuzelę 3D - ustawia render 3D i inicjalizuje kolekcje.
        /// </summary>
        public Carousel3DView()
        {
            InitializeComponent();
            WorldModels.Content = _group;
        }

        #region Dependency Properties

        /// <summary>
        /// Property kolekcji filmów do wyświetlenia w karuzeli.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<Film>), typeof(Carousel3DView), 
                new PropertyMetadata(null, OnItemsSourceChanged));

        /// <summary>
        /// Pobiera lub ustawia kolekcję filmów.
        /// </summary>
        public ObservableCollection<Film> ItemsSource
        {
            get => (ObservableCollection<Film>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Property wybranego na środku filmu.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(Film), typeof(Carousel3DView), 
                new PropertyMetadata(null, OnSelectedItemChanged));

        /// <summary>
        /// Pobiera lub ustawia aktualnie wybrany film.
        /// </summary>
        public Film SelectedItem
        {
            get => (Film)GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        #endregion

        /// <summary>
        /// Obsługuje zmianę kolekcji filmów - odbudowuje scenę 3D.
        /// </summary>
        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Carousel3DView)d;
            control.RebuildScene();
            if (e.NewValue is INotifyCollectionChanged newCollection)
                newCollection.CollectionChanged += (s, args) => control.RebuildScene();
        }

        /// <summary>
        /// Obsługuje zmianę wybranego filmu - aktualizuje animację pozycji.
        /// </summary>
        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (Carousel3DView)d;
            control.UpdatePositions(true);
        }

        /// <summary>
        /// Odbudowuje caŁą scenę 3D - czysćci i przesuwaj ponownie geometrię dla wszystkich filmów.
        /// </summary>
        private void RebuildScene()
        {
            _group.Children.Clear();
            _filmModels.Clear();

            if (ItemsSource == null) return;

            int index = 0;
            foreach (var film in ItemsSource)
            {
                var material = CreateSmartMaterial(film.PosterUri, film.Title, index);
                var geometry = CreateMesh();
                
                var model = new GeometryModel3D(geometry, material)
                {
                    BackMaterial = new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(255, 30, 30, 30)))
                };

                var transformGroup = new Transform3DGroup();
                
                // 1. Obrót
                var rotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0);
                transformGroup.Children.Add(new RotateTransform3D(rotation)); 
                
                // 2. Przesunięcie
                transformGroup.Children.Add(new TranslateTransform3D(0, 0, 0));
                
                model.Transform = transformGroup;

                _group.Children.Add(model);
                _filmModels[film] = model;
                index++;
            }

            UpdatePositions(false);
        }

        /// <summary>
        /// Aktualizuje pozycje wszystkich filmów - umieszcza wybrany w środku, inne z boków.
        /// </summary>
        /// <param name="animate">True = użyj animacji przejścia, False = zmiana natychmiastowa.</param>
        private void UpdatePositions(bool animate)
        {
            if (ItemsSource == null || SelectedItem == null || _filmModels.Count == 0) return;

            int selectedIndex = ItemsSource.IndexOf(SelectedItem);
            if (selectedIndex < 0) selectedIndex = 0;

            double centerGap = 3.5;
            double spacing = 1.5;
            double zDepth = 3.0;
            double rotationAngle = 60;

            for (int i = 0; i < ItemsSource.Count; i++)
            {
                var film = ItemsSource[i];
                if (!_filmModels.ContainsKey(film)) continue;

                var transformGroup = _filmModels[film].Transform as Transform3DGroup;
                if (transformGroup == null) continue; // <-- Dodaj to zabezpieczenie

                var rotateTrans = transformGroup.Children[0] as RotateTransform3D;
                if (rotateTrans == null) continue;
                var rotationAxis = rotateTrans.Rotation as AxisAngleRotation3D;
                if (rotationAxis == null) continue;
                var translateTrans = transformGroup.Children[1] as TranslateTransform3D;
                if (translateTrans == null) continue;

                int offset = i - selectedIndex;
                double targetX, targetZ, targetAngle;

                if (offset == 0)
                {
                    targetX = 0;
                    targetZ = 1.5;
                    targetAngle = 0;
                }
                else
                {
                    double sign = Math.Sign(offset);
                    double absOffset = Math.Abs(offset);

                    targetX = sign * (centerGap + ((absOffset - 1) * spacing));
                    targetZ = -zDepth - (absOffset * 0.2); 
                    targetAngle = (offset < 0) ? rotationAngle : -rotationAngle;
                }

                if (animate)
                {
                    var duration = TimeSpan.FromSeconds(0.5);
                    var easing = new CubicEase { EasingMode = EasingMode.EaseOut };

                    var animX = new DoubleAnimation(targetX, duration) { EasingFunction = easing };
                    var animZ = new DoubleAnimation(targetZ, duration) { EasingFunction = easing };
                    var animAngle = new DoubleAnimation(targetAngle, duration) { EasingFunction = easing };

                    translateTrans.BeginAnimation(TranslateTransform3D.OffsetXProperty, animX);
                    translateTrans.BeginAnimation(TranslateTransform3D.OffsetZProperty, animZ);
                    rotationAxis.BeginAnimation(AxisAngleRotation3D.AngleProperty, animAngle);
                }
                else
                {
                    translateTrans.BeginAnimation(TranslateTransform3D.OffsetXProperty, null);
                    translateTrans.BeginAnimation(TranslateTransform3D.OffsetZProperty, null);
                    rotationAxis.BeginAnimation(AxisAngleRotation3D.AngleProperty, null);

                    translateTrans.OffsetX = targetX;
                    translateTrans.OffsetZ = targetZ;
                    rotationAxis.Angle = targetAngle;
                }
            }
        }

        private Material CreateSmartMaterial(string uriPath, string title, int index)
        {
            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(uriPath, UriKind.Absolute);
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();

                var brush = new ImageBrush(image);
                brush.Freeze();
                return new DiffuseMaterial(brush);
            }
            catch
            {
                return CreateDebugMaterial(title, index);
            }
        }

        private Material CreateDebugMaterial(string title, int index)
        {
            var border = new Border 
            { 
                Width = 256, Height = 380, 
                Background = Brushes.DarkRed,
                BorderBrush = Brushes.Gold, 
                BorderThickness = new Thickness(4),
                Padding = new Thickness(5)
            };
            
            var textBlock = new TextBlock 
            { 
                Text = $"{index}. {title}", 
                Foreground = Brushes.White, 
                FontSize = 20, 
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = System.Windows.VerticalAlignment.Center, 
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            border.Child = textBlock;

            border.Measure(new Size(256, 380));
            border.Arrange(new Rect(0, 0, 256, 380));

            RenderTargetBitmap bmp = new RenderTargetBitmap(256, 380, 96, 96, PixelFormats.Pbgra32);
            bmp.Render(border);
            bmp.Freeze();

            var brush = new ImageBrush(bmp);
            brush.Freeze();
            return new DiffuseMaterial(brush);
        }

        private MeshGeometry3D CreateMesh()
        {
            var mesh = new MeshGeometry3D();
            mesh.Positions.Add(new Point3D(-1, 1.5, 0)); 
            mesh.Positions.Add(new Point3D(-1, -1.5, 0)); 
            mesh.Positions.Add(new Point3D(1, -1.5, 0));  
            mesh.Positions.Add(new Point3D(1, 1.5, 0));   
            
            mesh.TextureCoordinates.Add(new Point(0, 0));
            mesh.TextureCoordinates.Add(new Point(0, 1));
            mesh.TextureCoordinates.Add(new Point(1, 1));
            mesh.TextureCoordinates.Add(new Point(1, 0));
            
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(3);
            
            mesh.Freeze();
            return mesh;
        }

        private void OnViewportMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(MainViewport);
            PointHitTestParameters pointParams = new PointHitTestParameters(mousePos);
            VisualTreeHelper.HitTest(MainViewport, null, ResultCallback, pointParams);
        }

        private HitTestResultBehavior ResultCallback(HitTestResult result)
        {
            if (result is RayMeshGeometry3DHitTestResult meshResult)
            {
                foreach (var kvp in _filmModels)
                {
                    if (kvp.Value == meshResult.ModelHit)
                    {
                        var clickedFilm = kvp.Key;
                        if (SelectedItem != clickedFilm)
                        {
                            SelectedItem = clickedFilm;
                        }
                        return HitTestResultBehavior.Stop;
                    }
                }
            }
            return HitTestResultBehavior.Continue;
        }
    }
}