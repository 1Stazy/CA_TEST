using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CinemaSystem.Desktop.Models;
using CinemaSystem.Desktop.ViewModels;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok wyboru miejsc - interaktywny pland sali z drag-to-select.
    /// </summary>
    /// <remarks>
    /// Użytkownik może kliknąć poszczególne miejsca lub przeciągnąć prostokąt.
    /// Zaznaczone miejsca są podsumowane (ilosc, cena) na dole ekranu.
    /// </remarks>
    public partial class SeatSelectionView : System.Windows.Controls.UserControl
    {
        private bool _isDragging = false;
        private System.Windows.Point _startPoint;
        /// <summary>
        /// Zapamiętujemy stan miejsc PRZED rozpoczęciem zaznaczania.
        /// </summary>
        private List<Seat> _initialSelectionState = new();

        /// <summary>
        /// Inicjalizuje widok wyboru miejsc.
        /// </summary>
        public SeatSelectionView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Obsługuje naciśniċ przycisku myszy - uruchamia drag-to-select.
        /// </summary>
        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SeatSelectionViewModel vm) return;

            // W trybie edycji (bilet) blokujemy drag-to-select
            if (vm.IsEditMode) return;

            // Sprawdzamy, czy nie kliknięto bezpośrednio w przycisk fotela.
            // Jeśli kliknięto w fotel, to powinien zadziałać normalny Command przycisku.
            if (e.OriginalSource is DependencyObject source && FindParent<System.Windows.Controls.Button>(source) != null)
            {
                return; 
            }

            _isDragging = true;
            _startPoint = e.GetPosition(SelectionArea);

            // Zapamiętujemy, które miejsca były zaznaczone PRZED rozpoczęciem ciągnięcia
            _initialSelectionState = vm.Seats
                .Where(s => s.Status == SeatStatus.Selected)
                .ToList();

            // Ustawiamy prostokąt
            SelectionBox.Visibility = Visibility.Visible;
            SelectionBox.Width = 0;
            SelectionBox.Height = 0;
            Canvas.SetLeft(SelectionBox, _startPoint.X);
            Canvas.SetTop(SelectionBox, _startPoint.Y);

            SelectionArea.CaptureMouse();
        }

        /// <summary>
        /// Obsługuje ruch myszy - rysuje prostokąt zaznaczenia i aktualizuje miejsca.
        /// </summary>
        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;

            // 1. Rysowanie prostokąta
            var currentPoint = e.GetPosition(SelectionArea);

            var x = Math.Min(currentPoint.X, _startPoint.X);
            var y = Math.Min(currentPoint.Y, _startPoint.Y);
            var w = Math.Abs(currentPoint.X - _startPoint.X);
            var h = Math.Abs(currentPoint.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionBox, x);
            Canvas.SetTop(SelectionBox, y);
            SelectionBox.Width = w;
            SelectionBox.Height = h;

            // 2. Aktualizacja zaznaczenia
            UpdateSelection(new Rect(x, y, w, h));
        }

        /// <summary>
        /// Obsługuje puścienie przycisku myszy - kończy drag i aktualizuje podsumowanie.
        /// </summary>
        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                SelectionBox.Visibility = Visibility.Collapsed;
                SelectionArea.ReleaseMouseCapture();
                
                // Po zakończeniu odświeżamy podsumowanie (cenę)
                if (DataContext is SeatSelectionViewModel vm)
                {
                    vm.RecalculateSummary();
                }
            }
        }

        /// <summary>
        /// Aktualizuje zaznaczenie miejsc w prostokacie - jeśli miejsce jest w nim, zaznacz.
        /// </summary>
        private void UpdateSelection(Rect selectionRect)
        {
            if (DataContext is not SeatSelectionViewModel vm) return;

            // Iterujemy po kontenerach w ItemsControl
            for (int i = 0; i < SeatsControl.Items.Count; i++)
            {
                var container = SeatsControl.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                if (container == null) continue;

                var seat = SeatsControl.Items[i] as Seat;
                if (seat == null || seat.Status == SeatStatus.Taken) continue;

                // Sprawdzamy położenie przycisku względem obszaru zaznaczania
                var transform = container.TransformToAncestor(SelectionArea);
                var seatBounds = transform.TransformBounds(new Rect(0, 0, container.ActualWidth, container.ActualHeight));

                bool isIntersecting = selectionRect.IntersectsWith(seatBounds);

                if (isIntersecting)
                {
                    // W obszarze -> ZAZNACZ
                    vm.SetSeatSelectionState(seat, true);
                }
                else
                {
                    // Poza obszarem -> PRZYWRÓĆ STAN PIERWOTNY
                    // (Jeśli był zaznaczony przed akcją, to zostaje. Jeśli nie był, to odznaczamy)
                    bool wasSelectedOriginally = _initialSelectionState.Contains(seat);
                    vm.SetSeatSelectionState(seat, wasSelectedOriginally);
                }
            }
        }

        /// <summary>
        /// Metoda pomocnicza - znajduje rodzica (parent) określonego typu.
        /// </summary>
        public static T? FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}