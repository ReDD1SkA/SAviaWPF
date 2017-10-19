using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Drawing;
using SA.GCode;

namespace SAviaWPF
{
    public partial class MainWindow : Window
    {
        public CommandFile File { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            LogListBox.Items.Add(DateTime.Now + " - Ожидание выбора файла...");
        }

        private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = "Файлы команд (*.tap)|*.tap"
            };
            if (ofd.ShowDialog() == false)
                return;

            // Готовимся к работе с файлом: создаем объекты, очищаем элементы формы
            BeforeListBox.Items.Clear();
            AfterListBox.Items.Clear();
            BeforeTextBox.Clear();
            AfterTextBox.Clear();
            BeforeImage.Source = null;
            AfterImage.Source = null;

            File = new CommandFile();

            // Читаем файл, выводим листинг "До"
            await Task.Factory.StartNew(() => File.ReadFile(ofd.FileName));

            foreach (var line in File.Listing) BeforeListBox.Items.Add(line);
            BeforeTextBox.Text = File.GetTotalLength().ToString();

            LogListBox.Items.Add(DateTime.Now + " - Открыт файл: " + ofd.FileName + ".");
            LogListBox.Items.Add(DateTime.Now + " - Обработка файла...");

            // Рисуем график "До"
            Bitmap bmp = DrawGraph();

            BeforeImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
               bmp.GetHbitmap(),
               IntPtr.Zero,
               Int32Rect.Empty,
               BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));

            // Оптимизируем содержимое файла
            await Task.Factory.StartNew(File.OptimizeBlocksParallel);

            // Выводим листинг "После"
            foreach (var line in File.Listing) AfterListBox.Items.Add(line);
            AfterTextBox.Text = File.GetTotalLength().ToString();

            // Рисуем график "После"
            bmp = DrawGraph();

            AfterImage.Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
               bmp.GetHbitmap(),
               IntPtr.Zero,
               Int32Rect.Empty,
               BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));

            LogListBox.Items.Add(DateTime.Now + " - Обработка файла завершена.");
            SaveFileButton.IsEnabled = true;
        }

        private async void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Файлы команд (*.tap)|*.tap",
                DefaultExt = "*.tap"
            };
            if (sfd.ShowDialog() == false)
                return;

            await Task.Factory.StartNew(() => File.WriteToFile(sfd.FileName));

            LogListBox.Items.Add(DateTime.Now + " - Результаты сохранены в файл: " + sfd.FileName + ".");
        }

        private Bitmap DrawGraph()
        {
            List<Color> colors = new List<Color>
                {
                    Color.Red,
                    Color.Green,
                    Color.Blue,
                    Color.Purple,
                    Color.DeepPink,
                    Color.Orange,
                    Color.SandyBrown,
                    Color.LightBlue,
                    Color.DarkCyan,
                    Color.DarkOliveGreen
                };

            const int SCALE = 3, WIDTH = 320 * SCALE, HEIGHT = 220 * SCALE;

            Bitmap bmp = new Bitmap(WIDTH, HEIGHT);
            Graphics graph = Graphics.FromImage(bmp);

            Pen pen = new Pen(Color.LightGray, 1)
            {
                EndCap = System.Drawing.Drawing2D.LineCap.DiamondAnchor,
                StartCap = System.Drawing.Drawing2D.LineCap.DiamondAnchor
            };

            // Рисуем сетку
            for (int i = 10 * SCALE; i < WIDTH; i += 10 * SCALE) graph.DrawLine(pen, i, 0, i, HEIGHT - 1);
            for (int i = 10 * SCALE; i < HEIGHT; i += 10 * SCALE) graph.DrawLine(pen, 0, i, WIDTH - 1, i);

            pen.Color = Color.Gray;

            for (int i = 50 * SCALE; i < WIDTH; i += 50 * SCALE) graph.DrawLine(pen, i, 0, i, HEIGHT - 1);
            for (int i = 50 * SCALE; i < HEIGHT; i += 50 * SCALE) graph.DrawLine(pen, 0, i, WIDTH - 1, i);

            graph.DrawRectangle(pen, 0, 0, WIDTH - 1, HEIGHT - 1);

            // Рисуем собственно график
            int i_color = 0;

            foreach (var block in File.Blocks)
            {
                try
                {
                    pen.Color = colors[i_color];
                }
                catch (IndexOutOfRangeException)
                {
                    LogListBox.Items.Add(DateTime.Now + " - Ошибка! Не хватило цветов.");
                }

                for (int i = 0; i < block.Commands.Count - 1; i++)
                {
                    graph.DrawLine(pen,
                        (float)(SCALE * block.Commands[i].X),
                        (float)(SCALE * block.Commands[i].Y),
                        (float)(SCALE * block.Commands[i + 1].X),
                        (float)(SCALE * block.Commands[i + 1].Y)
                        );
                }

                i_color++;
            }

            return bmp;
        }
    }
}
