using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FilelistGenerator
{
    public partial class MainForm : Form
    {
        #region 文件名比较
        [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
        private static extern int StrCmpLogicalW(string psz1, string psz2);

        class StrComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }
        }
        #endregion

        string buttonText = string.Empty;
        int enumerateCount = 0;

        public MainForm()
        {
            InitializeComponent();

            saveFileDialog1.Filter = "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
            saveFileDialog1.FileName = "文件清单";
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            buttonText = button1.Text;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK != folderBrowserDialog1.ShowDialog(this))
                return;

            backgroundWorker1.RunWorkerAsync(folderBrowserDialog1.SelectedPath);
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            enumerateCount = 0;
            var completed = false;
            Exception exception = null;

            var task = Task.Factory.StartNew((selectedPath) =>
            {
                e.Result = Build(selectedPath.ToString());
            }, e.Argument.ToString()).ContinueWith((x) =>
            {
                completed = true;
                if (x.IsFaulted)
                {
                    exception = x.Exception.GetBaseException();
                }
            });

            var lastCount = 0;
            while (!completed)
            {
                if (enumerateCount > lastCount)
                {
                    lastCount = enumerateCount;
                    backgroundWorker1.ReportProgress(lastCount);
                }
                Thread.Sleep(100);
            }
            backgroundWorker1.ReportProgress(enumerateCount);

            if (exception != null)
            {
                this.Hide();
                throw exception;
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            button1.Text = buttonText + e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result == null)
                return;

            if (DialogResult.OK == saveFileDialog1.ShowDialog(this))
            {
                File.WriteAllText(saveFileDialog1.FileName, e.Result.ToString(), Encoding.UTF8);
                Process.Start(saveFileDialog1.FileName);
                this.Close(); //打开文件后关闭程序
            }
        }

        private string Build(string folder)
        {
            var root = new FilelistItem
            {
                Path = folder,
                FileType = FileType.Folder,
                Level = 0,
                Parent = null
            };

            var buffer = new List<FilelistItem>();
            buffer.Add(root);

            BuildInternal(root, buffer);

            StringBuilder sb = new StringBuilder(buffer.Count);
            foreach (var item in buffer)
            {
                var name = Path.GetFileName(item.Path);
                if (item.FileType == FileType.Folder)
                {
                    name += "/";
                }

                var current = item;
                if (current.Parent != null)
                {
                    name = (current.IsLast ? "└── " : "├── ") + name;

                    current = current.Parent;
                    while (current.Parent != null)
                    {
                        name = (current.IsLast ? "    " : "│	") + name;
                        current = current.Parent;
                    }
                }

                sb.AppendLine(name);
            }

            return sb.ToString();
        }

        private void BuildInternal(FilelistItem parent, List<FilelistItem> buffer)
        {
            FilelistItem lastFolder = null;
            foreach (var item in Directory.GetDirectories(parent.Path).OrderBy(x => x, new StrComparer()))
            {
                var folder = new FilelistItem
                {
                    Path = item,
                    FileType = FileType.Folder,
                    Level = parent.Level + 1,
                    Parent = parent
                };
                buffer.Add(folder);

                BuildInternal(folder, buffer);

                lastFolder = folder;

                enumerateCount++;
            }

            FilelistItem lastFile = null;
            foreach (var item in Directory.GetFiles(parent.Path).OrderBy(x => x, new StrComparer()))
            {
                var file = new FilelistItem
                {
                    Path = item,
                    FileType = FileType.File,
                    Level = parent.Level + 1,
                    Parent = parent
                };
                buffer.Add(file);

                lastFile = file;

                enumerateCount++;
            }

            if (lastFile != null)
            {
                lastFile.IsLast = true;
            }
            else if (lastFolder != null)
            {
                lastFolder.IsLast = true;
            }
        }

        enum FileType
        {
            Folder = 1,
            File = 2
        }

        class FilelistItem
        {
            public string Path { get; set; }
            /// <summary>
            /// 1-文件夹，2-文件
            /// </summary>
            public FileType FileType { get; set; }
            /// <summary>
            /// 文件层级，从0开始
            /// </summary>
            public int Level { get; set; }
            public bool IsLast { get; set; }
            public FilelistItem Parent { get; set; }
        }
    }
}
