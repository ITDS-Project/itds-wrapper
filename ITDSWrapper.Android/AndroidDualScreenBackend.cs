using System;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
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
    private int prevLength;
    
    public bool IsActive { get; set; } = false;
    public event Action? ActiveChanged;
    public event Action<float?, float?>? OnPress;
    public event Action? OnRelease;

    public void Initialize(Context outerContext, Display dualScreen)
    {
        _presentation = new(outerContext, dualScreen);
        _presentation.SetCancelable(false);
        _presentation.Window?.AddFlags(WindowManagerFlags.NotFocusable | WindowManagerFlags.KeepScreenOn);
        _presentation.Window?.SetBackgroundDrawable(new ColorDrawable(Color.Black));
        _view = new(_presentation.Context);
        _frame = Bitmap.CreateBitmap(256, 192, Bitmap.Config.Argb8888!);
        _view.SetImageBitmap(_frame);
        _view.SetBackgroundColor(Color.Black);
        _view.SetScaleType(ImageView.ScaleType.FitCenter);
        _view.Touch += (_, args) =>
        {
            if (args.Event?.Action == MotionEventActions.Down)
            {
                float[] point = [args.Event!.GetX(), args.Event.GetY()];
                Matrix inverse = new();
                _view.ImageMatrix?.Invert(inverse);
                inverse?.MapPoints(point);
                OnPress?.Invoke(point[0], point[1] + 192);
                args.Handled = true;
            }
            else if (args.Event?.Action == MotionEventActions.Up)
            {
                OnRelease?.Invoke();
                args.Handled = true;
            }
        };
        _presentation.SetContentView(_view);
        _presentation.Show();
        IsActive = true;
        ActiveChanged?.Invoke();
    }
    
    public void SetSecondFrame(byte[] pixels, uint width, uint height)
    {
        if (_pixBuffer is null || pixels.Length != prevLength)
            _pixBuffer = ByteBuffer.Allocate(pixels.Length);
        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (pixels[(y * width + x) * 4], pixels[(y * width + x) * 4 + 2]) = (pixels[(y * width + x) * 4 + 2], pixels[(y * width + x) * 4]);
            }
        }

        _pixBuffer.Position(0);
        _pixBuffer.Put(pixels);
        _pixBuffer.Position(0);
        _frame?.CopyPixelsFromBuffer(_pixBuffer);
        Dispatcher.UIThread.Post(() => _view?.SetImageBitmap(_frame));
    }
}