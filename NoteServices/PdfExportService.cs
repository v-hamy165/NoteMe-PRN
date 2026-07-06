using NoteMe.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;

namespace NoteMe.Services
{
    public class PdfExportService
    {
        // Segoe UI co san tren moi may Windows va ho tro day du tieng Viet.
        private const string FontFamily = "Segoe UI";

        static PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public void ExportSummary(MeetingSummary summary, string noteTitle, string outputPath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(style => style.FontSize(11).FontFamily(FontFamily));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("BIÊN BẢN TÓM TẮT CUỘC HỌP")
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);

                        column.Item().PaddingTop(4).Text($"Ghi chú: {noteTitle}").FontSize(12);

                        if (!string.IsNullOrWhiteSpace(summary.AudioFileName))
                        {
                            column.Item().Text($"File ghi âm: {summary.AudioFileName}")
                                .FontSize(9)
                                .FontColor(Colors.Grey.Darken1);
                        }

                        column.Item().Text(
                                $"Tạo lúc: {summary.CreatedAt:dd/MM/yyyy HH:mm} — Model: {summary.ModelUsed}"
                            )
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);

                        column.Item()
                            .PaddingTop(8)
                            .LineHorizontal(1)
                            .LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingTop(12).Column(column =>
                    {
                        column.Spacing(14);

                        column.Item().Element(section =>
                            RenderSection(section, "1. Nội dung chính", summary.MainContent)
                        );

                        column.Item().Element(section =>
                            RenderBulletSection(section, "2. Các việc đã làm", summary.CompletedSteps)
                        );

                        column.Item().Element(section =>
                            RenderBulletSection(section, "3. Các bước tiếp theo", summary.NextSteps)
                        );

                        if (!string.IsNullOrWhiteSpace(summary.Transcript))
                        {
                            column.Item().Element(section =>
                                RenderSection(section, "4. Bản ghi lời thoại", summary.Transcript)
                            );
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(style => style.FontSize(9).FontColor(Colors.Grey.Darken1));
                        text.Span("NoteMe — Trang ");
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf(outputPath);
        }

        private static void RenderSection(IContainer container, string title, string content)
        {
            container.Column(column =>
            {
                column.Spacing(6);

                column.Item().Text(title).FontSize(13).Bold();

                column.Item().Text(
                    string.IsNullOrWhiteSpace(content) ? "(Không có nội dung)" : content.Trim()
                );
            });
        }

        private static void RenderBulletSection(IContainer container, string title, string lines)
        {
            var items = (lines ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

            container.Column(column =>
            {
                column.Spacing(6);

                column.Item().Text(title).FontSize(13).Bold();

                if (items.Count == 0)
                {
                    column.Item().Text("(Không có mục nào)");
                    return;
                }

                foreach (string item in items)
                {
                    column.Item().Row(row =>
                    {
                        row.ConstantItem(14).Text("•");
                        row.RelativeItem().Text(item);
                    });
                }
            });
        }
    }
}
