using System;
using System.Collections.Generic;
namespace DeviceLoad
{
    public interface IBlobDataReader
    {
        IEnumerable<string> ReadBlobLines(string blobName, ref long startOffset, uint count);

        IEnumerable<string> GetDevices();

        double GetSecondOffset(DateTime baseTime);

        void CloseStreamReaders();
    }
}
