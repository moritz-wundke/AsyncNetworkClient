using System;

namespace AsyncNetClient.Utils
{
    public static class RandomUtils
    {
        private static Random _rnd = new();
        
        public static void SetSeed(int seed)
        {
            _rnd = new Random(seed);
        }
        
        public static void Reset()
        {
            _rnd = new Random();
        }
        
        public static int NextInt(int min, int max)
        {
            return _rnd.Next(min, max);
        }
        
        public static int NextInt(int max)
        {
            return _rnd.Next(max);
        }
        
        public static int NextInt()
        {
            return _rnd.Next();
        }
        
        
        public static double NextDouble(double min, double max)
        {
            return min + _rnd.NextDouble() * (max - min);
        }
        
        public static double NextDouble(double max)
        {
            return _rnd.NextDouble() * max;
        }
        
        public static double NextDouble()
        {
            return _rnd.NextDouble();
        }
    }
}