using System;
using System.Windows.Media;

namespace HaengSungAOI_WPF.Services.UI
{
    public interface IImageDisplayService
    {
        /// <summary>
        /// Cập nhật hình ảnh cho một camera cụ thể.
        /// </summary>
        /// <param name="cameraName">Tên camera (Align, Inspect1, ...)</param>
        /// <param name="imageSource">Nguồn hình ảnh (BitmapSource hoặc tương đương)</param>
        void UpdateImage(string cameraName, ImageSource imageSource);

        /// <summary>
        /// Sự kiện xảy ra khi có hình ảnh mới cần hiển thị.
        /// </summary>
        event EventHandler<(string CameraName, ImageSource ImageSource)> ImageUpdated;
    }

    public class ImageDisplayService : IImageDisplayService
    {
        public event EventHandler<(string CameraName, ImageSource ImageSource)> ImageUpdated;

        public void UpdateImage(string cameraName, ImageSource imageSource)
        {
            ImageUpdated?.Invoke(this, (cameraName, imageSource));
        }
    }
}



