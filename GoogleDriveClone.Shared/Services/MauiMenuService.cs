namespace GoogleDriveClone.Shared.Services
{
    public class MauiMenuService
    {
        public event Action? UploadFileRequested;
        public event Action? SortAscendingRequested;
        public event Action? SortDescendingRequested;
        public event Action? ShowDataRequested;

        public void TriggerUploadFile()
        {
            UploadFileRequested?.Invoke();
        }

        public void TriggerSortAscending()
        {
            SortAscendingRequested?.Invoke();
        }

        public void TriggerSortDescending()
        {
            SortDescendingRequested?.Invoke();
        }

        public void TriggerShowData()
        {
            ShowDataRequested?.Invoke();
        }
    }
}
