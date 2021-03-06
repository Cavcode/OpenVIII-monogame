﻿using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenVIII.AV;

namespace OpenVIII.Movie
{
    public class Player : IDisposable
    {
        #region Fields

        public static readonly int[] LetterBox = { 101, 103, 104 };
        private static Files _files;

        private State _state;

        private Audio _audio;

        private bool _disposedValue;

        private bool _suppressDraw;

        private Texture2D _texture;

        private Video _video;

        #endregion Fields

        #region Destructors

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Player()
        {
            //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        #endregion Destructors

        #region Events

        public event EventHandler<State> StateChanged;

        #endregion Events

        #region Properties

        public int Id { get; set; }

        // To detect redundant calls
        public bool IsDisposed => _disposedValue;

        public State State
        {
            get => _state; private set
            {
                _state = value;
                StateChanged?.Invoke(this, value);
            }
        }

        #endregion Properties

        #region Methods

        public static Player Load(int id, bool overlayingModels = false)
        {
            Player player;
            if(_files == null)
                _files = Files.Instance;
            if (File.Exists(_files[id]))
            {
                player = new Player
                {
                    Id = id,
                    State = State.Load,
                    _video = Video.Load(_files[id]),
                    _audio = Audio.Load(_files[id]),
                    _suppressDraw = !overlayingModels
                };
            }
            else if (_files.ZZZ)
            {
                return null; // doesn't work.
                //ArchiveZzz a = (ArchiveZzz)ArchiveZzz.Load(Memory.Archives.ZZZ_OTHER);
                //var fd = a.ArchiveMap.GetFileData(Files[ID]);

                //AV.Audio ffccAudioFromZZZ = AV.Audio.Load(
                //    new AV.BufferData
                //    {
                //        DataSeekLoc = fd.Value.Offset,
                //        DataSize = fd.Value.UncompressedSize,
                //        HeaderSize = 0,
                //        Target = AV.BufferData.TargetFile.other_zzz
                //    },
                //    null, -1);

                //AV.Video ffccVideoFromZZZ = AV.Video.Load(
                //    new AV.BufferData
                //    {
                //        DataSeekLoc = fd.Value.Offset,
                //        DataSize = fd.Value.UncompressedSize,
                //        HeaderSize = 0,
                //        Target = AV.BufferData.TargetFile.other_zzz
                //    },
                //    null, -1);

                ////ffcc.Play(volume, pitch, pan);

                //Player = new Player()
                //{
                //    ID = ID,
                //    STATE = STATE.LOAD,
                //    Video = ffccVideoFromZZZ,
                //    Audio = ffccAudioFromZZZ,
                //    SuppressDraw = !OverlayingModels
                //};
            }
            else
                return null;
            player.State++;
            if (player._video == null && player._audio == null)
                return null;
            return player;
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() =>
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);

        public void Draw()
        {
            switch (State)
            {
                case State.Load:
                    break;

                case State.Clear:
                    State++;
                    ClearScreen();
                    break;

                case State.StartPlay:
                case State.Playing:
                    PlayingDraw();
                    break;

                case State.Paused:
                    break;

                case State.Finished:
                    State++;
                    PlayingDraw();
                    break;

                case State.Reset:
                    break;
                case State.Return:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void PlayingDraw()
        {
            if (_texture == null)
            {
                return;
            }
            //draw frame;
            Memory.SpriteBatchStartStencil(ss: SamplerState.AnisotropicClamp);//by default xna filters all textures SamplerState.PointClamp disables that. so video is being filtered why playing.
            ClearScreen();
            var dst = new Rectangle(new Point(0), (new Vector2(_texture.Width, _texture.Height) * Memory.Scale(_texture.Width, _texture.Height, LetterBox.Contains(Id) ? ScaleMode.FitHorizontal : ScaleMode.FitBoth)).ToPoint());
            dst.Offset(Memory.Center.X - dst.Center.X, Memory.Center.Y - dst.Center.Y);
            Memory.SpriteBatch.Draw(_texture, dst, Color.White);
            Memory.SpriteBatchEnd();
        }

        public void Stop()
        {
            _audio.Dispose();
            _video.Dispose();
            State = State.Return;
        }

        public void Update()
        {
            switch (State)
            {
                case State.Load:
                    break;

                case State.Clear:
                    break;

                case State.StartPlay:
                    State++;
                    if (_audio != null)
                    {
                        if (Memory.Threaded)
                            _audio.PlayInTask();
                        else
                            _audio.Play();
                    }

                    _video?.Play();
                    break;

                case State.Playing:
                    if (_audio != null && !Memory.Threaded)
                    {
                        _audio.NextLoop();
                    }
                    if (_video == null)
                        State = State.Finished;
                    else if (_video.Behind)
                    {
                        if (_video.Next() < 0)
                        {
                            State = State.Finished;
                            //Memory.SuppressDraw = true;
                            break;
                        }

                        if (_texture != null)
                        {
                            _texture.Dispose();
                            GC.Collect();
                            GC.WaitForPendingFinalizers();
                            _texture = null;
                        }
                    }
                    else
                    {
                        //Memory next frame is skipped.
                        Memory.SuppressDraw = _suppressDraw;
                    }
                    if (_texture == null)
                    {
                        if (_video != null)
                        {
                            if (Memory.State?.FieldVars != null)
                                Memory.State.FieldVars.FMVFrames = (ulong)_video.CurrentFrameNum;
                            _texture = _video.Texture2D();
                        }
                    }
                    break;

                case State.Paused:
                    //todo add a function to pause sound
                    //pausing the stopwatch will cause the video to pause because it calculates the current frame based on time.
                    break;

                case State.Finished:
                    break;

                case State.Reset:
                    break;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposedValue) return;
            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
            if (!(_video?.IsDisposed ?? true))
                _video.Dispose();
            if (!(_audio?.IsDisposed ?? true))
                _audio.Dispose();
            if (_texture != null && !_texture.IsDisposed)
                _texture.Dispose();
            _disposedValue = true;
        }

        private static void ClearScreen() => Memory.SpriteBatch.GraphicsDevice.Clear(Color.Black);

        #endregion Methods

        // TODO: uncomment the following line if the finalizer is overridden above.// GC.SuppressFinalize(this);
    }
}