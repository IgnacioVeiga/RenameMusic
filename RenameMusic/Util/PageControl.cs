using RenameMusic.DB;
using System;

namespace RenameMusic.Util
{
    internal static class PageControl
    {
        internal static int PrimaryList_Page = 1;
        internal static int SecondaryList_Page = 1;
        internal static int FoldersList_Page = 1;

        internal static int PageSizeBox_SelctedItem = 5;

        internal static int PrimaryListTotalPages => GetTotalPages(
            DatabaseAPI.CountAudioItems(true), PageSizeBox_SelctedItem
            );
        internal static int SecondaryListTotalPages => GetTotalPages(
            DatabaseAPI.CountAudioItems(false), PageSizeBox_SelctedItem
            );
        internal static int FolderListTotalPages => GetTotalPages(
            DatabaseAPI.CountFolderItems(), PageSizeBox_SelctedItem
            );

        internal static int GetTotalPages(int totalItems, int pageSize)
        {
            int pages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (pages == 0) return 1;
            else return pages;
        }
    }
}
