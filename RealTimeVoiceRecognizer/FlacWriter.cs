using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RealTimeVoiceRecognizer
{
    internal class FlacWriter : IDisposable
    {
        #region Api
        const string Dll = "LibFlac";

        [DllImport(Dll)]
        static extern IntPtr FLAC__stream_encoder_new();

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_finish(IntPtr context);

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_delete(IntPtr context);

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_set_channels(IntPtr context, int value);

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_set_bits_per_sample(IntPtr context, int value);

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_set_sample_rate(IntPtr context, int value);

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_set_compression_level(IntPtr context, int value);

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_set_blocksize(IntPtr context, int value);

        [DllImport(Dll)]
        static extern int FLAC__stream_encoder_init_stream(IntPtr context, WriteCallback write, SeekCallback seek, TellCallback tell, MetadataCallback metadata, IntPtr userData);

        [DllImport(Dll)]
        static extern bool FLAC__stream_encoder_process_interleaved(IntPtr context, IntPtr buffer, int samples);

        // Callbacks
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int WriteCallback(IntPtr context, IntPtr buffer, int bytes, uint samples, uint current_frame, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int SeekCallback(IntPtr context, long absoluteOffset, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate int TellCallback(IntPtr context, out long absoluteOffset, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void MetadataCallback(IntPtr context, IntPtr metadata, IntPtr userData);
        #endregion

        #region Public methods

        public FlacWriter(Stream output, int bitDepth, int channels, int sampleRate)
        {
            stream = output;
            writer = new BinaryWriter(stream);

            inputBitDepth = bitDepth;
            inputChannels = channels;
            inputSampleRate = sampleRate;

            context = FLAC__stream_encoder_new();

            if (context == IntPtr.Zero)
                throw new ApplicationException("FLAC: Could not initialize stream encoder!");

            Check(
                FLAC__stream_encoder_set_channels(context, channels),
                "set channels");

            Check(
                FLAC__stream_encoder_set_bits_per_sample(context, bitDepth),
                "set bits per sample");

            Check(
                FLAC__stream_encoder_set_sample_rate(context, sampleRate),
                "set sample rate");

            Check(
                FLAC__stream_encoder_set_compression_level(context, 5),
                "set compression level");

            //Check(
            //    FLAC__stream_encoder_set_blocksize(context, 8192),
            //    "set block size");

            write = new WriteCallback(Write);
            seek = new SeekCallback(Seek);
            tell = new TellCallback(Tell);

            if (FLAC__stream_encoder_init_stream(context,
                                                 write, seek, tell,
                                                 null, IntPtr.Zero) != 0)
                throw new ApplicationException("FLAC: Could not open stream for writing!");
        }

        public void Dispose()
        {
            if (context != IntPtr.Zero)
            {
                Check(
                    FLAC__stream_encoder_finish(context),
                    "finalize stream encoder");

                Check(
                    FLAC__stream_encoder_delete(context),
                    "dispose of stream encoder instance");

                writer.Dispose();

                context = IntPtr.Zero;
            }
        }

        public void Close()
        {
            Dispose();
        }

        unsafe public void Write(byte[] buffer, int offset, int uncompressedBytes)
        {
            if (context == IntPtr.Zero)
                throw new ApplicationException("FLAC: Stream encoder is not initialized!");

            int bytes = inputBitDepth / 8;
            int paddedSamples = uncompressedBytes / bytes;
            int samples = paddedSamples / inputChannels;

            // 16/24-bit -> padding to a 32-bit integer
            if (padded == null || padded.Length < paddedSamples)
                padded = new int[paddedSamples];

            if (inputBitDepth == 16)
                for (int i = 0; i < paddedSamples; i++)
                    padded[i] = buffer[i * bytes + 1] << 8 |
                                buffer[i * bytes + 0];

            else if (inputBitDepth == 24)
                for (int i = 0; i < paddedSamples; i++)
                    padded[i] = buffer[i * bytes + 2] << 16 |
                                buffer[i * bytes + 1] << 8 |
                                buffer[i * bytes + 0];

            else
                throw new ApplicationException(string.Format("FLAC: Unsupported bit depth '{0}'!", inputBitDepth));

            fixed (int* fixedInput = padded)
            {
                IntPtr input = new IntPtr(fixedInput);

                Check(
                    FLAC__stream_encoder_process_interleaved(context, input, samples),
                    "process audio samples");
            }
        }

        #endregion

        #region Private methods / Callbacks

        private void Check(bool result, string operation)
        {
            if (!result)
                throw new ApplicationException(string.Format("FLAC: Could not {0}!", operation));
        }

        private int Write(IntPtr context, IntPtr buffer, int bytes, uint samples, uint current_frame, IntPtr userData)
        {
            // Allocate a 32-KB or [needed bytes] buffer, whichever is larger
            if (callbackBuffer == null || callbackBuffer.Length < bytes)
                callbackBuffer = new byte[Math.Max(bytes, 32 * 1024)];

            Marshal.Copy(buffer, callbackBuffer, 0, bytes);

            stream.Write(callbackBuffer, 0, bytes);

            return 0;
        }

        private int Seek(IntPtr context, long absoluteOffset, IntPtr userData)
        {
            stream.Position = absoluteOffset;

            return 0;
        }

        private int Tell(IntPtr context, out long absoluteOffset, IntPtr userData)
        {
            absoluteOffset = stream.Position;

            return 0;
        }

        #endregion

        #region Fields
        private IntPtr context;
        private Stream stream;
        private BinaryWriter writer;

        private int inputBitDepth;
        private int inputChannels;
        private int inputSampleRate;

        private int[] padded;
        private byte[] callbackBuffer;

        private WriteCallback write;
        private SeekCallback seek;
        private TellCallback tell;
        #endregion
    }
}
