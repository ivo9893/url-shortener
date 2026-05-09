using System.Text;

namespace URLShortener.Services
{
    public class IdGeneratorService
    {
        private readonly int _workerId;
        private long _lastTimestamp = -1;
        private long _sequence = 0;
        private readonly object _lock = new object();

        private const long Epoch = 1577836800000; // January 1, 2020
        private const int WorkerIdBits = 10;
        private const int SequenceBits = 12;
        private const long MaxSquence = 4096;
        private const string Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";


        public IdGeneratorService(IConfiguration configuration)
        {
            _workerId = configuration.GetValue<int>("SNOWFLAKE_WORKER_ID");

            if (_workerId < 0)
            {
                throw new ArgumentException("Worker id must be greater than 0");
            }

        }

        public (long id, string shortCode) GenerateShortCode()
        {
            long id = GenerateId();
            return (id, ToBase62(id));
        }

        private string ToBase62(long number)
        {
            if (number == 0) return "0";

            var result = new StringBuilder();

            while (number > 0)
            {
                int remainder = (int)(number % 62);
                result.Insert(0, Base62Chars[remainder]);
                number /= 62;
            }

            return result.ToString();
        }

        public long GenerateId()
        {

            lock (_lock)
            {
                var currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (currentTimestamp < _lastTimestamp)
                    throw new InvalidOperationException("current timestamp is older than lastTimeStamp");

                if (currentTimestamp == _lastTimestamp)
                {
                    _sequence = (_sequence + 1);
                    if (_sequence >= MaxSquence)
                    {
                        _sequence = 0;
                        currentTimestamp = WaitForNextMillisecond(currentTimestamp);
                    }
                }
                else
                {
                    _sequence = 0;
                }

                _lastTimestamp = currentTimestamp;

                var id = ((currentTimestamp - Epoch) << (WorkerIdBits + SequenceBits))
           | (_workerId << SequenceBits)
           | _sequence;

                return id;

            }
        }

        private long WaitForNextMillisecond(long lastTimestamp)
        {
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            while (timestamp <= lastTimestamp)
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
            return timestamp;
        }

    }
}
