using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace AudioInterface
{
    public class AudioSample : WaveStream
    {
        // General Sample Settings (Info)
        string _fileName = "";
        bool _loop;
        long _pausePosition = -1;
        bool _pauseLoop;

        // Sample WaveStream Settings
        WaveOffsetStream offsetStream;
        WaveChannel32 channelStream;
        bool muted;
        float volume;

        public AudioSample(string fileName)
        {
            _fileName = fileName;
            WaveFileReader reader = new WaveFileReader(fileName);
            offsetStream = new WaveOffsetStream(reader);
            channelStream = new WaveChannel32(offsetStream);
            muted = false;
            volume = 1.0f;
        }
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Check if the stream has been set to loop
            if (_loop)
            {
                // Looping code taken from NAudio Demo
                int read = 0;
                while (read < count)
                {
                    int required = count - read;
                    int readThisTime = channelStream.Read(buffer, offset + read, required);
                    if (readThisTime < required)
                    {
                        channelStream.Position = 0;
                    }

                    if (channelStream.Position >= channelStream.Length)
                    {
                        channelStream.Position = 0;
                    }
                    read += readThisTime;
                }
                return read;
            }
            else
            {
                // Normal read code, sample has not been set to loop
                return channelStream.Read(buffer, offset, count);
            }
        }
    }
}
