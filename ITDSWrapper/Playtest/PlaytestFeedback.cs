// ReSharper disable InconsistentNaming
namespace ITDSWrapper.Playtest;

internal enum FeedbackSentiment
{
    Positive,
    Negative,
    Problem,
}

internal record PlaytestFeedback(string Id, string Sentiment, string Comment, bool Screenshot);