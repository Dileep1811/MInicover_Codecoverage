using System.Collections.Generic;

namespace MiniCover.CommandLine
{
    public interface IOption
    {
        string Template { get; }
        string Description { get; }
    }

    public interface INoValueOption : IOption
    {
        void ReceiveValue(bool value);
    }

    public interface ISingleValueOption : IOption
    {
        void ReceiveValue(string value);
    }

    public interface IMultiValueOption : IOption
    {
        void ReceiveValue(IList<string> values);
    }
}