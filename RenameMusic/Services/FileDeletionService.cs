using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace RenameMusic.Services
{
    public interface IFileDeletionService
    {
        void DeleteFile(string filePath);
        void DeleteDirectory(string directoryPath);
    }

    public sealed class FileDeletionService : IFileDeletionService
    {
        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be empty.", nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                return;
            }

            FileSystem.DeleteFile(
                filePath,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.ThrowException);
        }

        public void DeleteDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new ArgumentException("Directory path cannot be empty.", nameof(directoryPath));
            }

            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            FileSystem.DeleteDirectory(
                directoryPath,
                UIOption.OnlyErrorDialogs,
                RecycleOption.SendToRecycleBin,
                UICancelOption.ThrowException);
        }
    }
}
