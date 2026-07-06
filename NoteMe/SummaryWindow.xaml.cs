using Microsoft.Win32;
using NoteMe.Models;
using NoteMe.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace NoteMe
{
    public partial class SummaryWindow : Window
    {
        private class SummarySource
        {
            public int AudioId { get; set; }

            public string Display { get; set; } = string.Empty;

            public override string ToString() => Display;
        }

        private readonly SummaryService summaryService = new SummaryService();
        private readonly AudioService audioService = new AudioService();
        private readonly PdfExportService pdfExportService = new PdfExportService();

        private readonly int noteId;
        private readonly string noteTitle;
        private readonly int userId;

        public SummaryWindow(int noteId, string noteTitle, int userId)
        {
            InitializeComponent();

            this.noteId = noteId;
            this.noteTitle = noteTitle;
            this.userId = userId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtNoteTitle.Text = $"Ghi chú: {noteTitle}";

            LoadSources();
            LoadSummaries();
        }

        private void LoadSources()
        {
            var sources = new List<SummarySource>
            {
                new SummarySource
                {
                    AudioId = 0,
                    Display = "— Tóm tắt từ nội dung văn bản của ghi chú —"
                }
            };

            sources.AddRange(
                audioService.GetAudiosByNote(noteId, userId)
                    .Select(a => new SummarySource
                    {
                        AudioId = a.Id,
                        Display = $"Ghi âm: {a.FileName}"
                    })
            );

            cboSource.ItemsSource = sources;
            cboSource.SelectedIndex = sources.Count > 1 ? 1 : 0;
        }

        private void LoadSummaries(int? selectSummaryId = null)
        {
            var summaries = summaryService.GetSummariesByNote(noteId, userId);

            dgSummaries.ItemsSource = summaries;

            if (selectSummaryId.HasValue)
            {
                dgSummaries.SelectedItem =
                    summaries.FirstOrDefault(s => s.Id == selectSummaryId.Value);
            }
            else if (summaries.Any())
            {
                dgSummaries.SelectedIndex = 0;
            }
            else
            {
                ShowSummaryDetail(null);
            }
        }

        private async void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            if (cboSource.SelectedItem is not SummarySource source)
            {
                MessageBox.Show("Vui lòng chọn nguồn để tóm tắt.");
                return;
            }

            btnGenerate.IsEnabled = false;
            cboSource.IsEnabled = false;
            progressBar.Visibility = Visibility.Visible;
            txtStatus.Text = source.AudioId == 0
                ? "Đang gửi nội dung ghi chú cho Gemini phân tích..."
                : "Đang gửi file ghi âm cho Gemini phân tích (audio dài có thể mất vài phút)...";

            try
            {
                MeetingSummary summary = source.AudioId == 0
                    ? await summaryService.CreateFromTextAsync(noteId, userId)
                    : await summaryService.CreateFromAudioAsync(noteId, source.AudioId, userId);

                LoadSummaries(summary.Id);
                txtStatus.Text = "Đã tạo và lưu bản tóm tắt thành công.";
            }
            catch (Exception ex)
            {
                txtStatus.Text = "Tạo tóm tắt thất bại.";
                MessageBox.Show(ex.Message, "Lỗi tạo tóm tắt",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnGenerate.IsEnabled = true;
                cboSource.IsEnabled = true;
                progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void dgSummaries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ShowSummaryDetail(dgSummaries.SelectedItem as MeetingSummary);
        }

        private void ShowSummaryDetail(MeetingSummary? summary)
        {
            txtMainContent.Text = summary?.MainContent ?? string.Empty;
            txtCompletedSteps.Text = summary?.CompletedSteps ?? string.Empty;
            txtNextSteps.Text = summary?.NextSteps ?? string.Empty;
            txtTranscript.Text = summary?.Transcript ?? string.Empty;
        }

        private void btnExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (dgSummaries.SelectedItem is not MeetingSummary summary)
            {
                MessageBox.Show("Vui lòng chọn bản tóm tắt cần xuất PDF.");
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PDF file (*.pdf)|*.pdf",
                FileName = BuildPdfFileName(summary)
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                pdfExportService.ExportSummary(summary, noteTitle, dialog.FileName);

                MessageBoxResult result = MessageBox.Show(
                    $"Đã xuất PDF thành công:\n{dialog.FileName}\n\nBạn có muốn mở file không?",
                    "Xuất PDF",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi xuất PDF",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteSummary_Click(object sender, RoutedEventArgs e)
        {
            if (dgSummaries.SelectedItem is not MeetingSummary summary)
            {
                MessageBox.Show("Vui lòng chọn bản tóm tắt cần xóa.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa bản tóm tắt này không?",
                "Xác nhận xóa",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            if (summaryService.DeleteSummary(summary.Id, userId))
            {
                LoadSummaries();
                txtStatus.Text = "Đã xóa bản tóm tắt.";
            }
            else
            {
                MessageBox.Show("Bản tóm tắt không còn tồn tại.");
            }
        }

        private string BuildPdfFileName(MeetingSummary summary)
        {
            string safeTitle = string.Concat(
                noteTitle.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c)
            );

            return $"TomTat_{safeTitle}_{summary.CreatedAt:yyyyMMdd_HHmm}.pdf";
        }
    }
}
