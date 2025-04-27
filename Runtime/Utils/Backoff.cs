using System;

namespace AsyncNetClient.Utils
{
    public class Backoff
    {
        private const double JitterMinDeviation = 0.0f;
        private const double JitterMaxDeviation = 1.0f;
        public const int LinealFactor = 1;

        private readonly double _maxBackoff;
        private readonly double _minBackoff;
        private readonly double _factor;

        public int Attempts { get; private set; }

        public Backoff(double min, double max, double factor = LinealFactor)
        {
            _minBackoff = min;
            _maxBackoff = max;
            _factor = factor;
            Attempts = 0;
        }

        public double NewAttempt(bool jitter)
        {
            var duration = CalculateAttemptDuration(jitter);
            Attempts++;
            return duration;
        }

        private double CalculateAttemptDuration(bool jitter)
        {
            if (Attempts == 0)
            {
                return 0;
            }

            var duration = _factor > LinealFactor ? _minBackoff * Math.Pow(_factor, Attempts) : _minBackoff;
            if (jitter)
            {
                duration = ApplyJitter(duration);
            }

            duration = FitInBounds(duration);
            return duration;
        }

        private double ApplyJitter(double duration) => 
            RandomUtils.NextDouble(JitterMinDeviation, JitterMaxDeviation) * (duration - _minBackoff) + _minBackoff;
        
        private double FitInBounds(double duration)
        {
            duration = duration < _minBackoff ? _minBackoff : duration;
            return duration > _maxBackoff ? _maxBackoff : duration;
        }

        public void Reset() => Attempts = 0;
    }
}