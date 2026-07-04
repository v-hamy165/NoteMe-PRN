using NAudio.Wave;
using NoteMe.Data;
using NoteMe.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NoteMe.Services
{
    public class AudioService : IDisposable
    {
        private const string AudioFolderName = "Audios";

        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private WaveOutEvent? waveOut;
        private AudioFileReader? audioReader;
        private TaskCompletionSource<AudioRecording?>? stopRecordingTask;
        private string? currentFullPath;
        private string? currentRelativePath;
        private int currentNoteId;
        private int currentUserId;

        public bool IsRecording => waveIn != null;

        public List<AudioRecording> GetAudiosByNote(int noteId, int userId)
        {
            using var context = new NoteMeDbContext();

            return context.AudioRecordings
                .Where(a => a.NoteId == noteId && a.Note != null && a.Note.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToList();
        }

        // Ham ghi am
        public void StartRecording(int noteId, int userId)
        {
            if (IsRecording)
            {
                throw new InvalidOperationException("Dang ghi am, vui long dung truoc.");
            }

            using var context = new NoteMeDbContext();

            bool noteExists = context.Notes
                .Any(n => n.Id == noteId && n.UserId == userId);

            if (!noteExists)
            {
                throw new InvalidOperationException("Ghi chu khong ton tai hoac khong thuoc tai khoan nay.");
            }

            string audioFolder = Path.Combine(AppContext.BaseDirectory, AudioFolderName);
            Directory.CreateDirectory(audioFolder);

            string fileName = $"note_{noteId}_{DateTime.Now:yyyyMMdd_HHmmss}.wav";
            currentRelativePath = Path.Combine(AudioFolderName, fileName);
            currentFullPath = Path.Combine(audioFolder, fileName);
            currentNoteId = noteId;
            currentUserId = userId;
            stopRecordingTask = new TaskCompletionSource<AudioRecording?>();

            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(44100, 1)
            };

            writer = new WaveFileWriter(currentFullPath, waveIn.WaveFormat);

            waveIn.DataAvailable += (sender, e) =>
            {
                writer?.Write(e.Buffer, 0, e.BytesRecorded);
                writer?.Flush();
            };

            waveIn.RecordingStopped += OnRecordingStopped;
            waveIn.StartRecording();
        }

        // Ham dung ghi am
        public Task<AudioRecording?> StopRecordingAsync()
        {
            if (!IsRecording || stopRecordingTask == null)
            {
                throw new InvalidOperationException("Chua co file ghi am nao dang chay.");
            }

            waveIn?.StopRecording();

            return stopRecordingTask.Task;
        }

        // Ham phat lai audio
        public void PlayAudio(int audioId, int noteId, int userId)
        {
            using var context = new NoteMeDbContext();

            var audio = context.AudioRecordings
                .Where(a => a.Id == audioId &&
                            a.NoteId == noteId &&
                            a.Note != null &&
                            a.Note.UserId == userId)
                .Select(a => new
                {
                    a.FilePath,
                    a.FileName
                })
                .FirstOrDefault();

            if (audio == null)
            {
                throw new InvalidOperationException("Khong tim thay file ghi am cua ghi chu nay.");
            }

            string fullPath = GetFullAudioPath(audio.FilePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("File ghi am khong con ton tai.", audio.FileName);
            }

            StopPlayback();

            audioReader = new AudioFileReader(fullPath);
            waveOut = new WaveOutEvent();
            waveOut.Init(audioReader);
            waveOut.Play();
        }

        public void StopPlayback()
        {
            waveOut?.Stop();
            waveOut?.Dispose();
            audioReader?.Dispose();

            waveOut = null;
            audioReader = null;
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            waveIn?.Dispose();
            writer?.Dispose();

            waveIn = null;
            writer = null;

            try
            {
                if (e.Exception != null)
                {
                    throw e.Exception;
                }

                if (string.IsNullOrWhiteSpace(currentFullPath) ||
                    string.IsNullOrWhiteSpace(currentRelativePath) ||
                    !File.Exists(currentFullPath) ||
                    new FileInfo(currentFullPath).Length <= 44)
                {
                    DeleteFileIfExists(currentFullPath);
                    stopRecordingTask?.SetResult(null);
                    ClearRecordingState();
                    return;
                }

                using var context = new NoteMeDbContext();

                Note? note = context.Notes
                    .FirstOrDefault(n => n.Id == currentNoteId && n.UserId == currentUserId);

                if (note == null)
                {
                    DeleteFileIfExists(currentFullPath);
                    stopRecordingTask?.SetResult(null);
                    ClearRecordingState();
                    return;
                }

                AudioRecording recording = new AudioRecording
                {
                    NoteId = note.Id,
                    FilePath = currentRelativePath,
                    FileName = Path.GetFileName(currentRelativePath),
                    CreatedAt = DateTime.Now
                };

                context.AudioRecordings.Add(recording);
                context.SaveChanges();

                stopRecordingTask?.SetResult(recording);
            }
            catch (Exception exception)
            {
                DeleteFileIfExists(currentFullPath);
                stopRecordingTask?.SetException(exception);
            }
            finally
            {
                ClearRecordingState();
            }
        }

        private static string GetFullAudioPath(string filePath)
        {
            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }

            return Path.Combine(AppContext.BaseDirectory, filePath);
        }

        private static void DeleteFileIfExists(string? filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        private void ClearRecordingState()
        {
            currentFullPath = null;
            currentRelativePath = null;
            currentNoteId = 0;
            currentUserId = 0;
        }

        public void Dispose()
        {
            if (IsRecording)
            {
                waveIn?.StopRecording();
            }

            StopPlayback();
            waveIn?.Dispose();
            writer?.Dispose();
        }
    }
}
