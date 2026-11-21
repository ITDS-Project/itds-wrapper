namespace ITDSWrapper.Graphics;

public interface IBorderLocator
{
    public string GetBorderPath(string name, BorderQuality quality);
}

public enum BorderQuality
{
    Q4K,
    Q1080,
    Q720,
}