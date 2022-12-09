using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Klrohias.NFast.ChartLoader.LargePez
{
    public class UnbufferedStreamReader : TextReader
    {
        private Stream stream;
        // 没有缓存是不可能的，这辈子都不可能的
        private byte[] buffer;
        private long bufferSize = 8192;
        private long basePosition = 0;
        private long bufferPosition = 0;
        public UnbufferedStreamReader(Stream stream)
        {
            this.stream = stream;
            MakeBuffer();
        }
        public UnbufferedStreamReader(Stream stream, int bufferSize)
        {
            this.stream = stream;
            this.bufferSize = bufferSize;
            MakeBuffer();
        }

        private void MakeBuffer()
        {
            basePosition = stream.Position;
            bufferPosition = 0;
            buffer = new byte[bufferSize];

            var count = bufferSize;
            if (stream.Position + bufferSize > stream.Length)
            {
                count = stream.Length - stream.Position;
            }

            stream.Read(buffer, 0, (int) count);
        }

        private byte readByte()
        {
            if (bufferPosition >= bufferSize)
            {
                MakeBuffer();
            }
            return buffer[bufferPosition++];
        }
        public override int Read()
        {
            return readByte();
        }

        public override void Close()
        {
            stream.Close();
        }
        protected override void Dispose(bool disposing)
        {
            stream.Dispose();
        }

        public override int Peek()
        {
            if (bufferPosition >= bufferSize)
            {
                var originPos = stream.Position;
                var result = stream.ReadByte();
                stream.Position = originPos;
                return result;
            }
            return buffer[bufferPosition];
        }

        public long Position
        {
            get => basePosition + bufferPosition;
            set
            {
                stream.Position = value;
                MakeBuffer();
            }
        }

        public bool EndOfStream => Position >= stream.Length;
    }
}