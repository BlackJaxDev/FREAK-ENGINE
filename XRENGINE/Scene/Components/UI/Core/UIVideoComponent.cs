using FFmpeg.AutoGen;
using System.Runtime.InteropServices;
using XREngine.Components.Scene;
using XREngine.Data;
using XREngine.Data.Rendering;

namespace XREngine.Rendering.UI
{
    /// <summary>
    /// Houses a viewport that renders a scene from a designated camera.
    /// </summary>
    public unsafe class UIVideoComponent : UIMaterialComponent
    {
        public AudioSourceComponent? AudioSource => GetSiblingComponent<AudioSourceComponent>();

        private readonly XRMaterialFrameBuffer _fbo;

        private string? _streamUrl;
        private AVFormatContext* _formatContext;
        private AVCodecContext* _videoCodecContext;
        private AVCodecContext* _audioCodecContext;

        private int _videoStreamIndex = -1;
        private int _audioStreamIndex = -1;
        //These bools are to prevent infinite pre-rendering recursion
        private bool _updating = false;
        private bool _swapping = false;
        private bool _rendering = false;
        
        public UIVideoComponent() : base(GetVideoMaterial())
        {
            _fbo = new XRMaterialFrameBuffer(Material);

            Engine.Time.Timer.SwapBuffers += SwapBuffers;
            //Engine.Time.Timer.UpdateFrame += Update;
            Engine.Time.Timer.RenderFrame += Render;
        }

        public XRTexture2D? VideoTexture => Material?.Textures[0] as XRTexture2D;

        private static XRMaterial GetVideoMaterial()
        {
            XRTexture2D texture = XRTexture2D.CreateFrameBufferTexture(1u, 1u,
                EPixelInternalFormat.Rgb8,
                EPixelFormat.Rgb,
                EPixelType.UnsignedByte,
                EFrameBufferAttachment.ColorAttachment0);
            //texture.SizedInternalFormat = ESizedInternalFormat.Rgb8;
            //texture.Resizable = false;
            return new XRMaterial([texture], XRShader.EngineShader(Path.Combine("Common", "UnlitTexturedForward.fs"), EShaderType.Fragment));
        }

        //protected override void OnResizeLayout(BoundingRectangle parentRegion)
        //{
        //    base.OnResizeLayout(parentRegion);

        //    int
        //        w = (int)ActualWidth.ClampMin(1.0f),
        //        h = (int)ActualHeight.ClampMin(1.0f);

        //    Viewport.Resize(w, h);
        //    _fbo.Resize(w, h);
        //}

        public void Update(XRCamera camera)
        {
            if (!IsActive || _updating)
                return;

            _updating = true;
            //Viewport.PreRenderUpdate();
            _updating = false;
        }
        public void SwapBuffers()
        {
            if (!IsActive || _swapping)
                return;

            _swapping = true;
            //Viewport.PreRenderSwap();
            _swapping = false;
        }
        public void Render()
        {
            if (!IsActive || _rendering)
                return;

            _rendering = true;
            //Viewport.Render(_fbo);
            _rendering = false;
        }

        public void Initialize()
        {
            ffmpeg.avdevice_register_all();
            ffmpeg.avformat_network_init();

            _formatContext = ffmpeg.avformat_alloc_context();

            AVDictionary* options = null;

            // Might need these for Twitch HLS:
            ffmpeg.av_dict_set(&options, "timeout", "10000000", 0);
            ffmpeg.av_dict_set(&options, "rw_timeout", "10000000", 0);

            fixed (AVFormatContext** p = &_formatContext)
                if (ffmpeg.avformat_open_input(p, _streamUrl, null, &options) != 0)
                {
                    Debug.LogWarning("Failed to open input stream");
                    return;
                }

            if (ffmpeg.avformat_find_stream_info(_formatContext, null) < 0)
            {
                Debug.LogWarning("Failed to find stream info");
                return;
            }

            for (int i = 0; i < _formatContext->nb_streams; i++)
            {
                var codecpar = _formatContext->streams[i]->codecpar;
                if (codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO && _videoStreamIndex < 0)
                    _videoStreamIndex = i;
                else if (codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO && _audioStreamIndex < 0)
                    _audioStreamIndex = i;
            }

            if (_videoStreamIndex == -1 && _audioStreamIndex == -1)
            {
                Debug.LogWarning("No video or audio stream found");
                return;
            }
            
            // Initialize Video Codec Context
            if (_videoStreamIndex >= 0)
            {
                var stream = _formatContext->streams[_videoStreamIndex];
                var codecPar = stream->codecpar;
                AVCodec* videoCodec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
                _videoCodecContext = ffmpeg.avcodec_alloc_context3(videoCodec);
                ffmpeg.avcodec_parameters_to_context(_videoCodecContext, codecPar);
                if (ffmpeg.avcodec_open2(_videoCodecContext, videoCodec, null) < 0)
                {
                    Debug.LogWarning("Failed to open video codec");
                    return;
                }
            }

            // Initialize Audio Codec Context
            if (_audioStreamIndex >= 0)
            {
                var stream = _formatContext->streams[_audioStreamIndex];
                var codecPar = stream->codecpar;
                AVCodec* audioCodec = ffmpeg.avcodec_find_decoder(codecPar->codec_id);
                _audioCodecContext = ffmpeg.avcodec_alloc_context3(audioCodec);
                ffmpeg.avcodec_parameters_to_context(_audioCodecContext, codecPar);
                if (ffmpeg.avcodec_open2(_audioCodecContext, audioCodec, null) < 0)
                {
                    Debug.LogWarning("Failed to open audio codec");
                    return;
                }
            }
        }

        public void StartProcessing()
        {
            AVPacket* packet = ffmpeg.av_packet_alloc();
            AVFrame* frame = ffmpeg.av_frame_alloc();

            while (ffmpeg.av_read_frame(_formatContext, packet) >= 0)
            {
                if (packet->stream_index == _videoStreamIndex)
                    DecodePacket(_videoCodecContext, packet, frame, isVideo: true);
                else if (packet->stream_index == _audioStreamIndex)
                    DecodePacket(_audioCodecContext, packet, frame, isVideo: false);
                
                ffmpeg.av_packet_unref(packet);
            }

            ffmpeg.av_frame_free(&frame);
            ffmpeg.av_packet_free(&packet);
        }

        private void DecodePacket(AVCodecContext* codecContext, AVPacket* packet, AVFrame* frame, bool isVideo)
        {
            int ret = ffmpeg.avcodec_send_packet(codecContext, packet);
            if (ret < 0 && ret != ffmpeg.AVERROR_EOF)
            {
                Debug.LogWarning("Error sending packet to codec");
                return;
            }

            while (ret >= 0)
            {
                ret = ffmpeg.avcodec_receive_frame(codecContext, frame);
                if (ret == ffmpeg.AVERROR(ffmpeg.EAGAIN) || ret == ffmpeg.AVERROR_EOF)
                {
                    return;
                }
                else if (ret < 0)
                {
                    Debug.LogWarning("Error receiving frame from codec");
                    return;
                }

                if (isVideo)
                {
                    // Here you have a decoded video frame in frame->data[0].
                    // The frame is typically in YUV format (e.g., YUV420P).
                    // You can convert it to RGB using swscale and display or process it.
                    ProcessVideoFrame(frame, codecContext);
                }
                else
                {
                    // Here you have decoded audio samples in frame->data[0], frame->nb_samples samples.
                    ProcessAudioFrame(frame, codecContext);
                }
            }
        }

        private void ProcessVideoFrame(AVFrame* frame, AVCodecContext* codecContext)
        {
            var mip = VideoTexture?.Mipmaps[0];
            if (mip is null)
                return;

            DataSource? data = mip.Data;
            if (data is null)
                return;

            // Create a SwsContext for converting the frame to RGB.
            SwsContext* swsContext = ffmpeg.sws_getContext(
                codecContext->width,
                codecContext->height,
                codecContext->pix_fmt,
                codecContext->width,
                codecContext->height,
                AVPixelFormat.AV_PIX_FMT_RGB24,
                ffmpeg.SWS_BILINEAR,
                null,
                null,
                null);

            // Allocate an AVFrame for the RGB output.
            AVFrame* rgbFrame = ffmpeg.av_frame_alloc();
            rgbFrame->width = codecContext->width;
            rgbFrame->height = codecContext->height;
            rgbFrame->format = (int)AVPixelFormat.AV_PIX_FMT_RGB24;
            // Allocate the buffer for the frame data.
            ffmpeg.av_frame_get_buffer(rgbFrame, 32);
            // Convert the frame to RGB.
            ffmpeg.sws_scale(swsContext, frame->data, frame->linesize, 0, codecContext->height, rgbFrame->data, rgbFrame->linesize);

            if (mip.Width != codecContext->width || mip.Height != codecContext->height)
                mip.Resize((uint)codecContext->width, (uint)codecContext->height, true);

            // Copy the RGB data to the texture.
            byte* src = rgbFrame->data[0];
            byte* dst = (byte*)data.Address;
            int srcStride = rgbFrame->linesize[0];
            int dstStride = (int)data.Length / codecContext->height;
            for (int y = 0; y < codecContext->height; y++)
            {
                Buffer.MemoryCopy(src, dst, dstStride, srcStride);
                src += srcStride;
                dst += dstStride;
            }

            mip.Invalidate();

            // Free the AVFrame.
            ffmpeg.av_frame_free(&rgbFrame);

            // Free the SwsContext.
            ffmpeg.sws_freeContext(swsContext);
        }

        private int _currentPboIndex = 0;
        private XRDataBuffer[] _pboBuffers =
        [
            new("", EBufferTarget.PixelUnpackBuffer, 1, EComponentType.Byte, 3, false, false) { Usage = EBufferUsage.StreamDraw },
            new("", EBufferTarget.PixelUnpackBuffer, 1, EComponentType.Byte, 3, false, false) { Usage = EBufferUsage.StreamDraw }
        ];

        private void UploadFrameWithPBO(byte[] frameData)
        {
            // Bind the PBO
            XRDataBuffer pbo = _pboBuffers[_currentPboIndex];

            //TODO: move double PBO uploading into XRTexture2D for easy PBO usage anywhere

            //pbo.Bind();
            pbo.MapBufferData();
            Marshal.Copy(frameData, 0, pbo.Address, frameData.Length);
            pbo.UnmapBufferData();

            VideoTexture?.Bind();
            // Now use TexSubImage2D with the PBO bound
            //GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, _width, _height, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
            // Unbind PBO
            //pbo.Unbind();

            // Swap PBO index for next frame
            _currentPboIndex = (_currentPboIndex + 1) % 2;
        }

        private void ProcessAudioFrame(AVFrame* frame, AVCodecContext* codecContext)
        {
            var audioSource = AudioSource;
            if (audioSource is null)
                return;

            short[] samples = new short[frame->nb_samples * codecContext->ch_layout.nb_channels];
            Marshal.Copy((IntPtr)frame->data[0], samples, 0, samples.Length);
            int freq = codecContext->sample_rate;
            bool stereo = codecContext->ch_layout.nb_channels == 2;
            audioSource.EnqueueStreamingBuffers(freq, stereo, samples);
        }

        public void Dispose()
        {
            if (_videoCodecContext != null)
            {
                fixed (AVCodecContext** p = &_videoCodecContext)
                {
                    ffmpeg.avcodec_free_context(p);
                }
            }
            if (_audioCodecContext != null)
            {
                fixed (AVCodecContext** p = &_audioCodecContext)
                {
                    ffmpeg.avcodec_free_context(p);
                }
            }
            if (_formatContext != null)
            {
                fixed (AVFormatContext** p = &_formatContext)
                {
                    ffmpeg.avformat_close_input(p);
                    ffmpeg.avformat_free_context(*p);
                }
            }
        }
    }
}
