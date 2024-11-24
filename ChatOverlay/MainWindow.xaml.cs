using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using NHotkey;
using NHotkey.Wpf;
using System.Windows.Input;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace ChatOverlay
{
    public class ChatMessage : INotifyPropertyChanged
    {
        public string Message { get; set; }
        public Brush RoleColor { get; set; }

        private bool _isNewMessage;
        public bool IsNewMessage
        {
            get { return _isNewMessage; }
            set
            {
                _isNewMessage = value;
                OnPropertyChanged(nameof(IsNewMessage));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string currentRole = "Blue";
        private readonly SolidColorBrush blueRole = new SolidColorBrush(Colors.Blue);
        private readonly SolidColorBrush redRole = new SolidColorBrush(Colors.Red);

        private ObservableCollection<ChatMessage> _chatMessages = new ObservableCollection<ChatMessage>();

        public ObservableCollection<ChatMessage> ChatMessages
        {
            get { return _chatMessages; }
        }

        public SolidColorBrush CurrentRoleColor
        {
            get
            {
                return currentRole == "Blue" ? blueRole : redRole;
            }
        }

        public SolidColorBrush CurrentRoleBackgroundColor
        {
            get
            {
                return currentRole == "Blue" ? new SolidColorBrush(Colors.Blue) { Opacity = 0.2 } : new SolidColorBrush(Colors.Red) { Opacity = 0.2 };
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = this;

            try
            {
                // Основные горячие клавиши
                HotkeyManager.Current.AddOrReplace("SelectBlueRole", Key.Scroll, ModifierKeys.None, (sender, e) => SetRole("Blue"));
                HotkeyManager.Current.AddOrReplace("SelectRedRole", Key.Pause, ModifierKeys.None, (sender, e) => SetRole("Red"));

                // Альтернативные горячие клавиши для цифровой панели
                HotkeyManager.Current.AddOrReplace("SelectRedRoleNumeric", Key.Multiply, ModifierKeys.None, (sender, e) => SetRole("Blue"));
                HotkeyManager.Current.AddOrReplace("SelectBlueRoleNumeric", Key.Divide, ModifierKeys.None, (sender, e) => SetRole("Red"));
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка регистрации горячих клавиш: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetRole(string role)
        {
            if (currentRole != role)
            {
                currentRole = role;
                OnPropertyChanged(nameof(CurrentRoleColor));
                OnPropertyChanged(nameof(CurrentRoleBackgroundColor));

                // Окно становится активным и панели появляются
                this.Activate();
                TopPanel.Visibility = Visibility.Visible;
                BottomPanel.Visibility = Visibility.Visible;
            }

            else
            {
                // Окно становится активным и панели появляются
                this.Activate();
                TopPanel.Visibility = Visibility.Visible;
                BottomPanel.Visibility = Visibility.Visible;
            }
        }

        private void SendMessage(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(MessageInput.Text))
            {
                return;
            }

            string message = MessageInput.Text;

            // Обрабатываем команду
            if (ProcessCommand(message))
            {
                MessageInput.Clear();
                return; // Если команда была обработана, не добавляем это сообщение в чат
            }

            // Если нет команд, просто добавляем сообщение в чат
            var newMessage = new ChatMessage
            {
                Message = MessageInput.Text,
                RoleColor = CurrentRoleColor,
                IsNewMessage = true // Устанавливаем флаг, чтобы активировать анимацию
            };

            // Добавляем новое сообщение в коллекцию
            _chatMessages.Add(newMessage);
            MessageInput.Clear();

            // Прокручиваем ListBox к последнему элементу
            ChatList.ScrollIntoView(newMessage);

            // Через 1 секунду сбрасываем флаг IsNewMessage
            Task.Delay(1000).ContinueWith(_ =>
            {
                newMessage.IsNewMessage = false;
            });
        }


        // Метод для обработки команд
        private bool ProcessCommand(string message)
        {
            // Проверка на команду /w
            if (message.StartsWith("/w "))
            {
                return ProcessWidthCommand(message);
            }

            // Проверка на команду /h
            if (message.StartsWith("/h "))
            {
                return ProcessHeightCommand(message);
            }

            // Проверка на команду /t
            if (message.StartsWith("/t "))
            {
                return ProcessTransparencyCommand(message);
            }

            // Проверка на команду /t
            if (message == "/r")
            {
                return ProcessResetCommand();
            }

            return false;
        }

        // Обработка команды /w
        private bool ProcessWidthCommand(string message)
        {
            if (int.TryParse(message.Substring(3).Trim(), out int width))
            {
                if (width >= 200)
                {
                    this.Width = width;
                }
                else
                {
                    ShowError("Ширина не может быть меньше 200.");
                }
                return true;
            }
            else
            {
                ShowError("Неверное значение ширины.");
                return true;
            }
        }

        // Обработка команды /h
        private bool ProcessHeightCommand(string message)
        {
            if (int.TryParse(message.Substring(3).Trim(), out int height))
            {
                if (height >= 200)
                {
                    this.Height = height;
                }
                else
                {
                    ShowError("Высота не может быть меньше 200.");
                }
                return true;
            }
            else
            {
                ShowError("Неверное значение высоты.");
                return true;
            }
        }

        // Обработка команды /t
        private bool ProcessTransparencyCommand(string message)
        {
            if (message.StartsWith("/t "))
            {
                if (int.TryParse(message.Substring(3).Trim(), out int transparency))
                {
                    if (transparency >= 0 && transparency <= 100)
                    {
                        // Получаем текущий цвет фона BorderWin
                        SolidColorBrush currentBrush = (SolidColorBrush)BorderWin.Background;
                        Color currentColor = currentBrush.Color;

                        // Рассчитываем новый альфа-канал на основе процента прозрачности
                        byte newAlpha = (byte)(transparency * 2.55); // Преобразуем от 0-100 в 0-255

                        // Создаем новый цвет с измененным альфа-каналом
                        Color newColor = Color.FromArgb(newAlpha, currentColor.R, currentColor.G, currentColor.B);

                        // Устанавливаем новый цвет как фон для Border
                        BorderWin.Background = new SolidColorBrush(newColor);
                    }
                    else
                    {
                        ShowError("Прозрачность должна быть от 0 до 100.");
                    }
                    return true;
                }
                else
                {
                    ShowError("Неверное значение прозрачности.");
                    return true;
                }
            }
            return false;
        }

        // Обработка команды /r
        private bool ProcessResetCommand()
        {
            this.Width = 350;
            this.Height = 500;
            BorderWin.Background = new SolidColorBrush(Color.FromArgb(122, 30, 30, 30));

            return true;
        }

        // Метод для вывода сообщений об ошибках
        private void ShowError(string errorMessage)
        {
            _chatMessages.Add(new ChatMessage
            {
                Message = errorMessage,
                RoleColor = new SolidColorBrush(Colors.Red)
            });
        }

        private void Window_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is Image))
            {
                this.DragMove();
            }
        }

        private void TopPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!(e.OriginalSource is Image))
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, MouseButtonEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage(sender, e);
            }
        }

        // Событие, когда окно становится активным
        private void Window_Activated(object sender, EventArgs e)
        {
            TopPanel.Visibility = Visibility.Visible;
            BottomPanel.Visibility = Visibility.Visible;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            TopPanel.Visibility = Visibility.Collapsed;
            BottomPanel.Visibility = Visibility.Collapsed;
        }

    }
}
