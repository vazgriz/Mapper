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

        IProgress<int> progressReporter;

        public ProgressWindow() {
            progressReporter = new Progress<int>(IncrementInternal);
            InitializeComponent();
        }

        public void SetMaximum(int value) {
            ProgressBar.Maximum = value;
            max = value;
        }

        public void Reset() {
            SetValue(0);
        }

        public void Increment() {
            progressReporter.Report(1);
        }

        void IncrementInternal(int value) {
            SetValue(progress + value);
        }

        void SetValue(int value) {
            progress = Math.Min(value, max);
            ProgressBar.Value = progress;
            ProgressLabel.Content = string.Format("{0} ({1}/{2})", text, progress, max);
        }

        public void SetText(string value) {
            text = value;
            ProgressLabel.Content = string.Format("{0} ({1}/{2})", text, progress, max);
        }
    }
}
