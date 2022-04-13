using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace Mapper {
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window {
        int max;
        int progress;
        string text;

        public ProgressWindow() {
            InitializeComponent();
        }

        public void SetMaximum(int value) {
            ProgressBar.Maximum = value;
            max = value;
        }

        public void Increment() {
            progress = Math.Min(progress + 1, max);
            ProgressBar.Value = progress;
            ProgressLabel.Content = string.Format("{0} ({1}/{2})", text, progress, max);
        }

        public void SetText(string value) {
            text = value;
            ProgressLabel.Content = string.Format("{0} ({1}/{2})", text, progress, max);
        }
    }
}
