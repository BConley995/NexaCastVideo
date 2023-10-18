using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexaCastVideo
{
    public class Spinner : IDisposable
    {
        private int _currentAnimationFrame;
        private readonly int _animationInterval = 100; // interval to switch spinner frame (100ms)
        private readonly Timer _timer;
        private bool _active;
        private readonly string[] _spinnerAnimationFrames = { "|", "/", "-", "\\" };

        public Spinner()
        {
            _timer = new Timer(Spin, null, Timeout.Infinite, _animationInterval);
        }

        private void Spin(object state)
        {
            lock (_timer)
            {
                if (_active)
                {
                    Console.Write($"\r{_spinnerAnimationFrames[_currentAnimationFrame]}");
                    _currentAnimationFrame++;
                    if (_currentAnimationFrame == _spinnerAnimationFrames.Length)
                    {
                        _currentAnimationFrame = 0;
                    }
                }
            }
        }

        public void Start()
        {
            lock (_timer)
            {
                _active = true;
                _timer.Change(0, _animationInterval);
            }
        }

        public void Stop()
        {
            lock (_timer)
            {
                _active = false;
                _timer.Change(Timeout.Infinite, _animationInterval);
                Console.Write("\r "); // overwrite spinner with space
            }
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }

}
