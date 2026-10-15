using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using Avalonia.Threading;
using ITDSWrapper.Graphics;
using Java.Nio;

namespace ITDSWrapper.Android;

public class AndroidDualScreenBackend : IDualScreenBackend
{
    private Presentation? _presentation;
    private ImageView? _view;
    private Bitmap? _frame;
    private ByteBuffer? _pixBuffer;
    
    public bool IsActive { get; set; } = false;
    public event Action? ActiveChanged;
    public event Action<float?, float?>? OnPress;
    public event Action? OnRelease;

    public void Initialize(Context outerContext, Display dualScreen)
    {
        _presentation = new(outerContext, dualScreen);
        _view = new(outerContext);
        _frame = Bitmap.CreateBitmap(256, 192, Bitmap.Config.Argb8888!);
        _view.SetImageBitmap(_frame);
        _view.SetBackgroundColor(Color.Black);
        _view.SetScaleType(ImageView.ScaleType.FitCenter);
        _view.Touch += (_, args) =>
        {
            if (args.Event?.Action == MotionEventActions.Down)
            {
                OnPress?.Invoke(args.Event?.RawX / _view.Width * 256, args.Event?.RawY / _view.Height * 192 + 192);
                args.Handled = true;
            }
            else if (args.Event?.Action == MotionEventActions.Up)
            {
                OnRelease?.Invoke();
                args.Handled = true;
            }
        };
        _presentation.AddContentView(_view, new(dualScreen.Width, dualScreen.Height));
        _presentation.Show();
        ActiveChanged?.Invoke();
        IsActive = true;
    }
    
    public void SetSecondFrame(byte[] pixels, uint width, uint height)
    {
        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (pixels[(y * width + x) * 4], pixels[(y * width + x) * 4 + 2]) = (pixels[(y * width + x) * 4 + 2], pixels[(y * width + x) * 4]);
            }
        }
        _pixBuffer = ByteBuffer.Wrap(pixels);
        _frame?.CopyPixelsFromBuffer(_pixBuffer);
        Dispatcher.UIThread.Invoke(() => _view?.SetImageBitmap(_frame));
    }
}