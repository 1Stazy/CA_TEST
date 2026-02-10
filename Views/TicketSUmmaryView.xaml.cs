using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CinemaSystem.Desktop.ViewModels;

namespace CinemaSystem.Desktop.Views
{
    /// <summary>
    /// Widok podsumowania koszyka biletów - editor drogi i finalizacja.
    /// </summary>
    /// <remarks>
    /// Pokazuje listę wybranych biletów, pozwala na ich usunięcie.
    /// Obsługuje drag-to-select dla zaznaczenia wielu pozycji na raz.
    /// Wyswietla czasowych kod rabatowy i cenę %— zzamiast się.
    /// </remarks>
    public partial class TicketSummaryView : System.Windows.Controls.UserControl
    {
        private bool _isDragging = false;
        /// <summary>
        /// Czy zaznaczamy (true) czy odznaczamy (false) podczas dragu.
        /// </summary>
        private bool _isAddingSelection = true;

        /// <summary>
        /// Inicjalizuje widok podsumowania.
        /// </summary>
        public TicketSummaryView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Obsługuje naciśniécie myszy na liśćę - uruchamia drag-to-select.
        /// </summary>
        private void OnListMouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = GetItemAtMouse(e.GetPosition(TicketsList));
            if (item != null)
            {
                _isDragging = true;
                // Jeśli klikniemy w zaznaczony -> będziemy odznaczać
                // Jeśli w niezaznaczony -> zaznaczać
                _isAddingSelection = !item.IsSelected;
                item.IsSelected = _isAddingSelection;
            }
        }

        /// <summary>
        /// Obsługuje ruch myszy - zaznacza/odznacza pozycje wedlug kierunku draugu.
        /// </summary>
        private void OnListMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var item = GetItemAtMouse(e.GetPosition(TicketsList));
                if (item != null)
                {
                    // Ustawiamy stan zgodny z rozpoczęciem ruchu
                    if (item.IsSelected != _isAddingSelection)
                    {
                        item.IsSelected = _isAddingSelection;
                    }
                }
            }
        }

        /// <summary>
        /// Obsługuje puścienie myszy - kończy drag select.
        /// </summary>
        private void OnListMouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
        }

        /// <summary>
        /// Znajduje element listy Pod pozycją myszy.
        /// </summary>
        private TicketCartItem? GetItemAtMouse(System.Windows.Point point)
        {
            var element = TicketsList.InputHitTest(point) as DependencyObject;
            var container = FindParent<ContentPresenter>(element);
            
            if (container != null && container.DataContext is TicketCartItem item)
            {
                return item;
            }
            return null;
        }

        /// <summary>
        /// Metoda pomocnicza - znajduje rodzica okreslónego typu w visual tree.
        /// </summary>
        public static T? FindParent<T>(DependencyObject? child) where T : DependencyObject
        {
            if (child == null) return null;
            DependencyObject? parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent) return parent;
            return FindParent<T>(parentObject);
        }
    }
}