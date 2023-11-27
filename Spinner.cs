using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexaCastVideo
{
    /// <summary>
    /// Spinner class provides a simple console-based spinner animation.
    /// It adheres to the SOLID principles, particularly Single Responsibility,
    /// by focusing solely on the spinner animation logic.
    /// </summary>
    public class Spinner : IDisposable
    {
        private int _currentAnimationFrame;
        private readonly int _animationInterval = 100;
        private readonly Timer _timer;
        private bool _active;
        private readonly string[] _spinnerAnimationFrames = { "|", "/", "-", "\\" };

        // Initializes a new instance of the Spinner class.
        public Spinner()
        {
            // Timer is used for handling the animation state
            _timer = new Timer(Spin, null, Timeout.Infinite, _animationInterval);
        }

        // Handles the animation of the spinner
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

        // Starts the spinner animation
        public void Start()
        {
            lock (_timer)
            {
                _active = true;
                _timer.Change(0, _animationInterval);
            }
        }

        // Stops the spinner animation
        public void Stop()
        {
            lock (_timer)
            {
                _active = false;
                _timer.Change(Timeout.Infinite, _animationInterval);
                Console.Write("\r "); 
            }
        }

        // Disposes the timer resource
        public void Dispose()
        {
            _timer.Dispose();
        }
    }

}
